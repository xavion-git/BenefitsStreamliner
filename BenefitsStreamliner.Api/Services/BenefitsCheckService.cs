using BenefitsStreamliner.Api.Data;
using BenefitsStreamliner.Core.DTOs;
using BenefitsStreamliner.Core.Interfaces;
using BenefitsStreamliner.Core.Models;
using BenefitsStreamliner.Core.Rules;
using Microsoft.EntityFrameworkCore;

namespace BenefitsStreamliner.Api.Services;

// The "Compare Against iBenefits" workflow, now using the real tuition refund rules.
public class BenefitsCheckService(
    AppDbContext db,
    IBenefitsService benefits,
    IFinanceService finance,
    RetryQueue retryQueue,
    IConfiguration config,
    ILogger<BenefitsCheckService> logger)
{
    public async Task<CheckBenefitsResult?> CheckAsync(string applicationId, CancellationToken ct = default)
    {
        // 1. Retrieve application data from the database
        var app = await db.Applications.FirstOrDefaultAsync(a => a.ApplicationId == applicationId, ct);
        if (app is null) return null;

        // Only new claims, queued retries and "Waiting for Hours" claims can be (re)checked.
        if (app.Status is not (ApplicationStatus.Submitted or ApplicationStatus.InReview or ApplicationStatus.WaitingForHours))
            return CheckBenefitsResult.From(app, "This application has already been decided.");

        var wasWaiting = app.Status == ApplicationStatus.WaitingForHours;
        if (!wasWaiting)
        {
            app.Status = ApplicationStatus.InReview;
            await db.SaveChangesAsync(ct);
        }

        // 2. Request benefit data from iBenefits (via the abstraction)
        BenefitsData data;
        try
        {
            data = await benefits.GetBenefitsDataAsync(app, ct);
        }
        catch (BenefitsUnavailableException ex)
        {
            // Alternative path: log error, queue retry
            logger.LogError(ex, "iBenefits unavailable for {Id}.", applicationId);

            // A monthly re-check that fails stays "Waiting for Hours" and is simply picked up again on the next cycle.
            if (wasWaiting)
                return CheckBenefitsResult.From(app, "iBenefits is unavailable right now. The hours re-check will be tried again shortly.");

            if (retryQueue.Enqueue(applicationId))
            {
                logger.LogWarning("Retry queued for {Id}.", applicationId);
                return CheckBenefitsResult.From(app,
                    "iBenefits is currently unavailable. Your check has been queued and will be retried automatically.");
            }

            app.Status = ApplicationStatus.Error;
            await db.SaveChangesAsync(ct);
            return CheckBenefitsResult.From(app, "iBenefits remained unavailable after several attempts. Please contact support.");
        }

        // 3. Compare and generate the recommendation
        var result = EligibilityRules.Evaluate(app, data, DateTime.UtcNow);
        var (status, decision) = result.Outcome switch
        {
            EligibilityOutcome.Eligible => (ApplicationStatus.PendingFinanceReview, "Eligible"),
            EligibilityOutcome.WaitingForHours => (ApplicationStatus.WaitingForHours, ApplicationStatus.WaitingForHours.Display()),
            _ => (ApplicationStatus.Denied, ApplicationStatus.Denied.Display())
        };

        app.Status = status;
        app.NextRecheckAt = status == ApplicationStatus.WaitingForHours
            ? NextRecheck(DateTime.UtcNow, EligibilityRules.HoursDeadline(app))
            : null;

        // 4. Store the recommendation
        var rec = new Recommendation { ApplicationId = app.ApplicationId, Decision = decision, Reason = result.Reason };
        db.Recommendations.Add(rec);
        await db.SaveChangesAsync(ct);

        // 5. Pass the recommendation to Finance
        await finance.SendRecommendationAsync(rec, ct);

        return CheckBenefitsResult.From(app, decision: decision, reason: result.Reason);
    }

    public async Task<CheckBenefitsResult?> GetStatusAsync(string applicationId)
    {
        var app = await db.Applications.FirstOrDefaultAsync(a => a.ApplicationId == applicationId);
        if (app is null) return null;

        var rec = await db.Recommendations
            .Where(r => r.ApplicationId == applicationId)
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();

        var message = app.Status == ApplicationStatus.InReview ? "Still waiting on iBenefits (retry queued)." : null;
        return CheckBenefitsResult.From(app, message, rec?.Decision, rec?.Reason);
    }

    // Monthly re-check. HoursRecheck:DemoIntervalMinutes shortens it for demos.
    // Never later than the day after the 6-month window closes, so the denial happens promptly.
    private DateTime NextRecheck(DateTime nowUtc, DateTime deadline)
    {
        var demoMinutes = config.GetValue<int>("HoursRecheck:DemoIntervalMinutes");
        var next = demoMinutes > 0 ? nowUtc.AddMinutes(demoMinutes) : nowUtc.AddMonths(1);
        var latest = deadline.AddDays(1);
        return next > latest ? latest : next;
    }
}

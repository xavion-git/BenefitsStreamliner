using BenefitsStreamliner.Api.Data;
using BenefitsStreamliner.Core.DTOs;
using BenefitsStreamliner.Core.Interfaces;
using BenefitsStreamliner.Core.Models;
using BenefitsStreamliner.Core.Rules;
using Microsoft.EntityFrameworkCore;

namespace BenefitsStreamliner.Api.Services;

// The system lane of the future-state flow:
// New -> duplicate check -> Reviewing (iBenefits + rules) -> Pending Approval / Waiting for Hours / Denied.
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
        if (app.Status is not (ApplicationStatus.New or ApplicationStatus.Reviewing or ApplicationStatus.WaitingForHours))
            return CheckBenefitsResult.From(app, "This application has already been processed.");

        // 2. Duplicate claim? Same certificate number as an earlier claim that isn't itself a duplicate.
        //    (For now the certificate number alone identifies the claim: there is no tuition code on the form.)
        if (app.Status == ApplicationStatus.New)
        {
            var original = await db.Applications
                .Where(a => a.CertificateNumber == app.CertificateNumber && a.Id < app.Id && a.Status != ApplicationStatus.Duplicate)
                .OrderBy(a => a.Id)
                .Select(a => a.ApplicationId)
                .FirstOrDefaultAsync(ct);

            if (original is not null)
            {
                app.Status = ApplicationStatus.Duplicate;
                app.DuplicateOfApplicationId = original;
                await db.SaveChangesAsync(ct);
                logger.LogWarning("{Id} is a duplicate of {Original}. Processing stopped.", applicationId, original);
                return CheckBenefitsResult.From(app,
                    $"A claim for this certificate number was already submitted ({original}). Processing has stopped.");
            }
        }

        var wasWaiting = app.Status == ApplicationStatus.WaitingForHours;
        if (!wasWaiting)
        {
            app.Status = ApplicationStatus.Reviewing;
            await db.SaveChangesAsync(ct);
        }

        // 3. Request benefit data from iBenefits (via the abstraction)
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

        // 4. Compare and generate the recommendation
        var result = EligibilityRules.Evaluate(app, data, DateTime.UtcNow);
        var (status, decision) = result.Outcome switch
        {
            EligibilityOutcome.Eligible => (ApplicationStatus.PendingApproval, "Eligible"),
            EligibilityOutcome.WaitingForHours => (ApplicationStatus.WaitingForHours, ApplicationStatus.WaitingForHours.Display()),
            _ => (ApplicationStatus.Denied, ApplicationStatus.Denied.Display())
        };

        app.Status = status;
        app.NextRecheckAt = status == ApplicationStatus.WaitingForHours
            ? NextRecheck(DateTime.UtcNow, EligibilityRules.HoursDeadline(app))
            : null;

        // 5. Store the recommendation
        var rec = new Recommendation { ApplicationId = app.ApplicationId, Decision = decision, Reason = result.Reason };
        db.Recommendations.Add(rec);
        await db.SaveChangesAsync(ct);

        // 6. Pass the recommendation on (Finance / Accounting)
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

        var message = app.Status switch
        {
            ApplicationStatus.Reviewing => "Still waiting on iBenefits (retry queued).",
            ApplicationStatus.Duplicate => $"A claim for this certificate number was already submitted ({app.DuplicateOfApplicationId}). Processing has stopped.",
            _ => null
        };
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

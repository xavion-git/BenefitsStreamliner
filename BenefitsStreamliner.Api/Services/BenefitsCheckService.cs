using BenefitsStreamliner.Api.Data;
using BenefitsStreamliner.Core;
using BenefitsStreamliner.Core.DTOs;
using BenefitsStreamliner.Core.Interfaces;
using BenefitsStreamliner.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BenefitsStreamliner.Api.Services;

public class BenefitsCheckService(
    AppDbContext db,
    IBenefitsService benefits,
    IFinanceService finance,
    RetryQueue retryQueue,
    ILogger<BenefitsCheckService> logger)
{
    public async Task<CheckBenefitsResult?> CheckAsync(string applicationId, CancellationToken ct = default)
    {
        // 1. Retrieve application data from the database
        var app = await db.Applications.FirstOrDefaultAsync(a => a.ApplicationId == applicationId, ct);
        if (app is null) return null;

        app.Status = ApplicationStatus.Processing;
        await db.SaveChangesAsync(ct);

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

            if (retryQueue.Enqueue(applicationId))
            {
                logger.LogWarning("Retry queued for {Id}.", applicationId);
                return new CheckBenefitsResult
                {
                    ApplicationId = applicationId,
                    Status = "RetryQueued",
                    Message = "iBenefits is currently unavailable. Your check has been queued and will be retried automatically."
                };
            }

            app.Status = ApplicationStatus.Error;
            await db.SaveChangesAsync(ct);
            return new CheckBenefitsResult
            {
                ApplicationId = applicationId,
                Status = "Error",
                Message = "iBenefits remained unavailable after several attempts. Please contact support."
            };
        }

        // 3. Compare and generate recommendation
        var (decision, reason) = RecommendationLogic.Evaluate(app, data, DateTime.UtcNow);

        // 4. Store recommendation
        var rec = new Recommendation { ApplicationId = app.ApplicationId, Decision = decision, Reason = reason };
        db.Recommendations.Add(rec);
        app.Status = ApplicationStatus.Completed;
        await db.SaveChangesAsync(ct);

        // 5. Pass recommendation to Finance
        await finance.SendRecommendationAsync(rec, ct);

        return new CheckBenefitsResult
        {
            ApplicationId = applicationId,
            Status = "Completed",
            Decision = decision,
            Reason = reason
        };
    }

    public async Task<CheckBenefitsResult?> GetStatusAsync(string applicationId)
    {
        var app = await db.Applications.FirstOrDefaultAsync(a => a.ApplicationId == applicationId);
        if (app is null) return null;

        var rec = await db.Recommendations
            .Where(r => r.ApplicationId == applicationId)
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();

        return new CheckBenefitsResult
        {
            ApplicationId = applicationId,
            Status = app.Status.ToString(),
            Decision = rec?.Decision,
            Reason = rec?.Reason,
            Message = app.Status == ApplicationStatus.Processing ? "Still waiting on iBenefits (retry queued)." : null
        };
    }
}
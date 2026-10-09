using BenefitsStreamliner.Api.Data;
using BenefitsStreamliner.Core.DTOs;
using BenefitsStreamliner.Core.Interfaces;
using BenefitsStreamliner.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BenefitsStreamliner.Api.Services;

public class DecisionService(AppDbContext db, IBusinessCentralService businessCentral, ILogger<DecisionService> logger)
{
    // Returns (null, null) if the application doesn't exist, (null, message) if it isn't ready for a decision.
    public async Task<(CheckBenefitsResult? Result, string? Error)> DecideAsync(
        string applicationId, bool approve, CancellationToken ct = default)
    {
        var app = await db.Applications.FirstOrDefaultAsync(a => a.ApplicationId == applicationId, ct);
        if (app is null) return (null, null);

        if (app.Status != ApplicationStatus.Completed)
            return (null, $"A decision needs a completed benefits check first (current status: {app.Status}).");

        if (!approve)
        {
            app.Status = ApplicationStatus.Rejected;
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Finance rejected {Id}", applicationId);
            return (new CheckBenefitsResult { ApplicationId = applicationId, Status = "Rejected",
                Message = "Application rejected by Finance." }, null);
        }

        app.Status = ApplicationStatus.Approved;
        await db.SaveChangesAsync(ct);

        var ok = await businessCentral.TransferApprovedApplicationAsync(app, ct);
        app.Status = ok ? ApplicationStatus.Transferred : ApplicationStatus.Error;
        await db.SaveChangesAsync(ct);

        return (new CheckBenefitsResult { ApplicationId = applicationId, Status = app.Status.ToString(),
            Message = ok ? "Approved and transferred to Business Central." : "Approved, but the transfer failed." }, null);
    }
}

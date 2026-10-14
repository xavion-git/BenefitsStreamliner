using BenefitsStreamliner.Api.Data;
using BenefitsStreamliner.Core.DTOs;
using BenefitsStreamliner.Core.Interfaces;
using BenefitsStreamliner.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BenefitsStreamliner.Api.Services;

// The approval step. The future-state flow has Accounting review, two approvals and a printed cheque;
// the prototype collapses that into one Approve / Reject decision plus a mock Business Central hand-off.
public class DecisionService(AppDbContext db, IBusinessCentralService businessCentral, ILogger<DecisionService> logger)
{
    // Returns (null, null) if the application doesn't exist, (null, message) if it isn't ready for a decision.
    public async Task<(CheckBenefitsResult? Result, string? Error)> DecideAsync(
        string applicationId, bool approve, CancellationToken ct = default)
    {
        var app = await db.Applications.FirstOrDefaultAsync(a => a.ApplicationId == applicationId, ct);
        if (app is null) return (null, null);

        if (app.Status != ApplicationStatus.PendingApproval)
            return (null, $"A decision needs a claim in \"{ApplicationStatus.PendingApproval.Display()}\" " +
                          $"(current status: {app.Status.Display()}).");

        if (!approve)
        {
            app.Status = ApplicationStatus.Denied;
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Finance denied {Id}", applicationId);
            return (CheckBenefitsResult.From(app, "Claim denied."), null);
        }

        app.Status = ApplicationStatus.Approved;
        await db.SaveChangesAsync(ct);

        var ok = await businessCentral.TransferApprovedApplicationAsync(app, ct);
        if (!ok)
        {
            app.Status = ApplicationStatus.Error;
            await db.SaveChangesAsync(ct);
        }

        return (CheckBenefitsResult.From(app, ok
            ? "Approved and sent to Business Central."
            : "Approved, but the transfer to Business Central failed."), null);
    }
}

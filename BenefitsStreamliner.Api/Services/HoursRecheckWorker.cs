using BenefitsStreamliner.Api.Data;
using BenefitsStreamliner.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BenefitsStreamliner.Api.Services;

// Wakes up regularly and re-checks every "Waiting for Hours" claim whose re-check date has arrived.
// A claim that still lacks hours after the 6-month window is Denied by EligibilityRules.
public class HoursRecheckWorker(IServiceScopeFactory scopes, IConfiguration config, ILogger<HoursRecheckWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var pollSeconds = Math.Max(5, config.GetValue("HoursRecheck:PollSeconds", 3600));

        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(TimeSpan.FromSeconds(pollSeconds), ct); }
            catch (OperationCanceledException) { break; }

            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var checks = scope.ServiceProvider.GetRequiredService<BenefitsCheckService>();

                var now = DateTime.UtcNow;
                var due = await db.Applications
                    .Where(a => a.Status == ApplicationStatus.WaitingForHours && a.NextRecheckAt <= now)
                    .Select(a => a.ApplicationId)
                    .ToListAsync(ct);

                foreach (var id in due)
                {
                    var result = await checks.CheckAsync(id, ct);
                    logger.LogInformation("Hours re-check for {Id} finished with status {Status}", id, result?.StatusLabel);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Hours re-check cycle failed");
            }
        }
    }
}

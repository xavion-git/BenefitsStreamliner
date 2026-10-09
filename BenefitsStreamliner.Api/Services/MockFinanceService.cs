using BenefitsStreamliner.Core.Interfaces;
using BenefitsStreamliner.Core.Models;

namespace BenefitsStreamliner.Api.Services;

public class MockFinanceService(ILogger<MockFinanceService> logger) : IFinanceService
{
    public Task SendRecommendationAsync(Recommendation r, CancellationToken ct = default)
    {
        logger.LogInformation("[FINANCE] Received recommendation for {Id}: {Decision} - {Reason}",
            r.ApplicationId, r.Decision, r.Reason);
        return Task.CompletedTask;
    }
}
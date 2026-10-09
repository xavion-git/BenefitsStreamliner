using BenefitsStreamliner.Core.Interfaces;
using BenefitsStreamliner.Core.Models;

namespace BenefitsStreamliner.Api.Services;

public class MockBusinessCentralService(ILogger<MockBusinessCentralService> logger) : IBusinessCentralService
{
    public Task<bool> TransferApprovedApplicationAsync(Application application, CancellationToken ct = default)
    {
        logger.LogInformation("[BUSINESS CENTRAL] Transferred approved application {Id}. Transfer result: Success",
            application.ApplicationId);
        return Task.FromResult(true);
    }
}

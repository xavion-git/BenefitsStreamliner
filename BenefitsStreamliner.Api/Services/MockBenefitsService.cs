using System.Collections.Concurrent;
using BenefitsStreamliner.Core.DTOs;
using BenefitsStreamliner.Core.Interfaces;
using BenefitsStreamliner.Core.Models;

namespace BenefitsStreamliner.Api.Services;

public class MockBenefitsService(IConfiguration config, ILogger<MockBenefitsService> logger) : IBenefitsService
{
    private readonly ConcurrentDictionary<string, int> _calls = new();

    public async Task<BenefitsData> GetBenefitsDataAsync(Application application, CancellationToken ct = default)
    {
        await Task.Delay(300, ct);   // pretend network latency
        var call = _calls.AddOrUpdate(application.ApplicationId, 1, (_, c) => c + 1);

        // Demo failure switches:
        //  - config MockBenefits:AlwaysUnavailable = true  -> always fails
        //  - last name "Unavailable"                       -> fails on the first call only, so the retry succeeds
        var alwaysDown = config.GetValue<bool>("MockBenefits:AlwaysUnavailable");
        var demoDown = application.LastName.Equals("Unavailable", StringComparison.OrdinalIgnoreCase) && call == 1;
        if (alwaysDown || demoDown)
            throw new BenefitsUnavailableException("Mock iBenefits is unavailable (simulated).");

        logger.LogInformation("Mock iBenefits returned data for {Id}", application.ApplicationId);
        return new BenefitsData
        {
            ApplicantId = application.ApplicationId,
            Eligible = true,
            BenefitAmount = 500,
            IncomeLimit = 60000,
            MinimumAge = 18
        };
    }
}
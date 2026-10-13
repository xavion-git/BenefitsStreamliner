using System.Collections.Concurrent;
using BenefitsStreamliner.Core.DTOs;
using BenefitsStreamliner.Core.Interfaces;
using BenefitsStreamliner.Core.Models;

namespace BenefitsStreamliner.Api.Services;

// Predictable iBenefits test data, keyed by certificate number.
//
//   CERT-1001  Valid, 320 hours                      -> Eligible
//   CERT-1002  Valid, 100 hours (+60 on each lookup) -> Waiting for Hours, then Eligible on the re-check
//   CERT-1003  Valid, 200 hours, out of benefit 9 mo -> needs 300 -> Waiting for Hours (never catches up)
//   CERT-1004  Valid, 250 hours, started on hire     -> needs 300 -> Waiting for Hours
//   CERT-1005  Benefit status Terminated             -> Denied
//   CERT-1006  Employer is not a member company      -> Denied
//   CERT-9999  Valid, 200 hours; unavailable on the first call only -> retry queue -> Eligible
//   anything else: not found in iBenefits            -> Denied
public class MockBenefitsService(IConfiguration config, ILogger<MockBenefitsService> logger) : IBenefitsService
{
    private readonly ConcurrentDictionary<string, int> _calls = new();

    private static BenefitsData Sample(string cert, decimal hours, string status = "Valid", int monthsOut = 0,
        bool onHire = false, bool member = true, bool fullTime = true) => new()
    {
        CertificateNumber = cert, Found = true, BenefitStatus = status, HoursWorked = hours,
        MonthsOutOfBenefit = monthsOut, StartedTrainingOnHire = onHire, EmployerIsMember = member, FullTime = fullTime
    };

    private static readonly Dictionary<string, BenefitsData> Samples = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CERT-1001"] = Sample("CERT-1001", 320),
        ["CERT-1002"] = Sample("CERT-1002", 100),
        ["CERT-1003"] = Sample("CERT-1003", 200, monthsOut: 9),
        ["CERT-1004"] = Sample("CERT-1004", 250, onHire: true),
        ["CERT-1005"] = Sample("CERT-1005", 400, status: "Terminated"),
        ["CERT-1006"] = Sample("CERT-1006", 400, member: false),
        ["CERT-9999"] = Sample("CERT-9999", 200),
    };

    public async Task<BenefitsData> GetBenefitsDataAsync(Application application, CancellationToken ct = default)
    {
        await Task.Delay(300, ct);   // pretend network latency

        var cert = application.CertificateNumber.Trim().ToUpperInvariant();
        var call = _calls.AddOrUpdate(cert, 1, (_, c) => c + 1);

        // Demo failure switches:
        //  - config MockBenefits:AlwaysUnavailable = true -> always fails
        //  - certificate CERT-9999                         -> fails on the first call only, so the retry succeeds
        var alwaysDown = config.GetValue<bool>("MockBenefits:AlwaysUnavailable");
        var demoDown = cert == "CERT-9999" && call == 1;
        if (alwaysDown || demoDown)
            throw new BenefitsUnavailableException("Mock iBenefits is unavailable (simulated).");

        if (!Samples.TryGetValue(cert, out var s))
        {
            logger.LogInformation("Mock iBenefits has no record for {Cert}", cert);
            return new BenefitsData { CertificateNumber = cert, Found = false };
        }

        // CERT-1002 gains 60 hours on every lookup, so the monthly re-check can succeed.
        var hours = cert == "CERT-1002" ? s.HoursWorked + 60 * (call - 1) : s.HoursWorked;

        logger.LogInformation("Mock iBenefits returned data for {Cert}: {Status}, {Hours} hours", cert, s.BenefitStatus, hours);
        return new BenefitsData
        {
            CertificateNumber = s.CertificateNumber, Found = true, BenefitStatus = s.BenefitStatus, HoursWorked = hours,
            MonthsOutOfBenefit = s.MonthsOutOfBenefit, StartedTrainingOnHire = s.StartedTrainingOnHire,
            EmployerIsMember = s.EmployerIsMember, FullTime = s.FullTime
        };
    }
}

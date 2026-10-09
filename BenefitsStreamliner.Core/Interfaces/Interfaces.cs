using BenefitsStreamliner.Core.DTOs;
using BenefitsStreamliner.Core.Models;

namespace BenefitsStreamliner.Core.Interfaces;

public interface IBenefitsService
{
    Task<BenefitsData> GetBenefitsDataAsync(Application application, CancellationToken ct = default);
}

public interface IFinanceService
{
    Task SendRecommendationAsync(Recommendation recommendation, CancellationToken ct = default);
}

public class BenefitsUnavailableException(string message) : Exception(message);
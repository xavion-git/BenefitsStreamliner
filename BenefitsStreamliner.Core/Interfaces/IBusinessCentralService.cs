using BenefitsStreamliner.Core.Models;

namespace BenefitsStreamliner.Core.Interfaces;

public interface IBusinessCentralService
{
    Task<bool> TransferApprovedApplicationAsync(Application application, CancellationToken ct = default);
}

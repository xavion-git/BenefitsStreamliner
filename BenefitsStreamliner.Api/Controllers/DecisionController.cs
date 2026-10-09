using BenefitsStreamliner.Api.Services;
using BenefitsStreamliner.Core.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BenefitsStreamliner.Api.Controllers;

[ApiController]
[Route("api/applications/{applicationId}/decision")]
public class DecisionController(DecisionService decisions) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CheckBenefitsResult>> Decide(string applicationId, DecisionRequest request)
    {
        var decision = request.Decision.Trim().ToLowerInvariant();
        if (decision is not ("approve" or "reject"))
            return BadRequest("Decision must be 'Approve' or 'Reject'.");

        var (result, error) = await decisions.DecideAsync(applicationId, decision == "approve");
        if (error is not null) return Conflict(error);
        return result is null ? NotFound() : Ok(result);
    }
}

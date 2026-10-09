using BenefitsStreamliner.Api.Services;
using BenefitsStreamliner.Core.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BenefitsStreamliner.Api.Controllers;

[ApiController]
[Route("api/applications")]
public class ApplicationsController(
    ApplicationService applications,
    BenefitsCheckService checks,
    ILogger<ApplicationsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApplicationResponse>> Submit(ApplicationRequest request)
    {
        // [ApiController] already returns 400 for DataAnnotation failures.
        if (request.DateOfBirth!.Value.Date >= DateTime.Today)
        {
            ModelState.AddModelError(nameof(request.DateOfBirth), "Date of birth must be in the past.");
            return ValidationProblem(ModelState);
        }

        try
        {
            var id = await applications.SubmitAsync(request);
            return Ok(new ApplicationResponse(id));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save application");
            return Problem("We couldn't save your application. Please try again.", statusCode: 500);
        }
    }

    [HttpPost("{applicationId}/check-benefits")]
    public async Task<ActionResult<CheckBenefitsResult>> CheckBenefits(string applicationId)
    {
        try
        {
            var result = await checks.CheckAsync(applicationId);
            return result is null ? NotFound() : Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Benefits check failed for {Id}", applicationId);
            return Problem("The benefits check failed unexpectedly.", statusCode: 500);
        }
    }

    [HttpGet("{applicationId}")]
    public async Task<ActionResult<CheckBenefitsResult>> GetStatus(string applicationId)
    {
        var result = await checks.GetStatusAsync(applicationId);
        return result is null ? NotFound() : Ok(result);
    }
}
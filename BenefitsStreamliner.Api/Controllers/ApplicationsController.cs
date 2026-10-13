using BenefitsStreamliner.Api.Services;
using BenefitsStreamliner.Core.DTOs;
using BenefitsStreamliner.Core.Rules;
using Microsoft.AspNetCore.Mvc;

namespace BenefitsStreamliner.Api.Controllers;

[ApiController]
[Route("api/applications")]
public class ApplicationsController(
    ApplicationService applications,
    BenefitsCheckService checks,
    ILogger<ApplicationsController> logger) : ControllerBase
{
    // multipart/form-data: the claim fields, plus files named "documents" (Marks and Proof of Completion,
    // one or more) and "proofOfPayment" (required for ON and BC).
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(100_000_000)]
    public async Task<ActionResult<ApplicationResponse>> Submit([FromForm] ApplicationRequest request, CancellationToken ct)
    {
        var documents = Request.Form.Files.GetFiles("documents");
        var proofOfPayment = Request.Form.Files.GetFile("proofOfPayment");

        // Same rules the Blazor form uses (SIN checksum, provinces, uploads, ...). The API is the authority.
        var errors = ClaimValidator.Validate(
            request,
            documents.Select(f => new FileMeta(f.FileName, f.Length)).ToList(),
            proofOfPayment is null ? null : new FileMeta(proofOfPayment.FileName, proofOfPayment.Length));

        if (errors.Count > 0)
        {
            foreach (var (field, message) in errors) ModelState.AddModelError(field, message);
            return ValidationProblem(ModelState);
        }

        try
        {
            var (id, duplicateOf) = await applications.SubmitAsync(request, documents, proofOfPayment, ct);

            if (id is null)
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Duplicate claim",
                    Detail = $"A claim for this certificate number and tuition code already exists ({duplicateOf})."
                });

            return Ok(new ApplicationResponse(id));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save application");   // never log the request: it contains a SIN
            return Problem("We couldn't save your claim. Please try again.", statusCode: 500);
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

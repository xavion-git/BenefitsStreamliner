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
    // multipart/form-data: the claim fields, plus files named "completionDocs" (Marks and Proof of Completion)
    // and "paymentDocs" (Proof of Payment, required when the training province is ON or BC).
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(150_000_000)]
    public async Task<ActionResult<ApplicationResponse>> Submit([FromForm] ApplicationRequest request, CancellationToken ct)
    {
        var completionDocs = Request.Form.Files.GetFiles("completionDocs");

        // Payment documents only count when the training province needs them (as on the form).
        IReadOnlyList<IFormFile> paymentDocs = ClaimValidator.ProofOfPaymentRequired(request)
            ? Request.Form.Files.GetFiles("paymentDocs")
            : Array.Empty<IFormFile>();

        // Same rules the Blazor form uses (SIN checksum, provinces, dates, uploads, ...). The API is the authority.
        var errors = ClaimValidator.Validate(
            request,
            completionDocs.Select(f => new FileMeta(f.FileName, f.Length)).ToList(),
            paymentDocs.Select(f => new FileMeta(f.FileName, f.Length)).ToList(),
            DateTime.Today);

        if (errors.Count > 0)
        {
            foreach (var (field, message) in errors) ModelState.AddModelError(field, message);
            return ValidationProblem(ModelState);
        }

        try
        {
            var id = await applications.SubmitAsync(request, completionDocs, paymentDocs, ct);
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

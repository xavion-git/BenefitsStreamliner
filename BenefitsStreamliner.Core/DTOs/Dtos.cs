ususing System.ComponentModel.DataAnnotations;

namespace BenefitsStreamliner.Core.DTOs;

public class ApplicationRequest
{
    [Required, StringLength(100)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(100)] public string LastName { get; set; } = string.Empty;
    [Required] public DateTime? DateOfBirth { get; set; }
    [Range(0, 10000000)] public decimal Income { get; set; }
}

public record ApplicationResponse(string ApplicationId);

public class BenefitsData
{
    public string ApplicantId { get; set; } = string.Empty;
    public bool Eligible { get; set; }
    public decimal BenefitAmount { get; set; }
    public decimal IncomeLimit { get; set; }
    public int MinimumAge { get; set; }
}

public class CheckBenefitsResult
{
    public string ApplicationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;   // Completed | RetryQueued | Processing | Error
    public string? Message { get; set; }
    public string? Decision { get; set; }
    public string? Reason { get; set; }
}
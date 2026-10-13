using BenefitsStreamliner.Core.Models;

namespace BenefitsStreamliner.Core.DTOs;

// Everything the claim form collects, except the files. Validation lives in Rules/ClaimValidator.cs
// so the API and the Blazor form use exactly the same rules.
public class ApplicationRequest
{
    public string? EmployeeName { get; set; }
    public string? Sin { get; set; }                  // 9 digits (the form joins its three boxes)
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? MailingAddress { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }

    public string? CompanyName { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyCity { get; set; }
    public string? CompanyProvince { get; set; }
    public string? CompanyPostalCode { get; set; }

    public string? Trade { get; set; }
    public int? TrainingYear { get; set; }
    public DateTime? TrainingFrom { get; set; }
    public DateTime? TrainingTo { get; set; }
    public DateTime? ReturnedToWork { get; set; }
    public string? PayTo { get; set; }                // Employer | Employee

    public string? CertificateNumber { get; set; }
    public string? TuitionCode { get; set; }

    public string? EmployeeSignature { get; set; }
    public DateTime? EmployeeSignDate { get; set; }
    public string? RepName { get; set; }
    public string? RepPosition { get; set; }
    public string? RepSignature { get; set; }
    public DateTime? RepSignDate { get; set; }
}

public record ApplicationResponse(string ApplicationId);

// What iBenefits returns for a certificate number.
public class BenefitsData
{
    public string CertificateNumber { get; set; } = string.Empty;
    public bool Found { get; set; }
    public string BenefitStatus { get; set; } = string.Empty;   // "Valid" is the only passing value
    public decimal HoursWorked { get; set; }
    public int MonthsOutOfBenefit { get; set; }
    public bool StartedTrainingOnHire { get; set; }
    public bool EmployerIsMember { get; set; }                  // OpenCircle member company
    public bool FullTime { get; set; }                          // returned to full-time work
}

public class CheckBenefitsResult
{
    public string ApplicationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;          // code, e.g. WaitingForHours
    public string StatusLabel { get; set; } = string.Empty;     // display, e.g. Waiting for Hours
    public string? Message { get; set; }
    public string? Decision { get; set; }
    public string? Reason { get; set; }
    public DateTime? NextRecheckAt { get; set; }

    public static CheckBenefitsResult From(Application app, string? message = null, string? decision = null, string? reason = null) => new()
    {
        ApplicationId = app.ApplicationId,
        Status = app.Status.ToString(),
        StatusLabel = app.Status.Display(),
        Message = message,
        Decision = decision,
        Reason = reason,
        NextRecheckAt = app.NextRecheckAt
    };
}

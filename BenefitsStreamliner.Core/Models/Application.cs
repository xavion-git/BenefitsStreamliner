namespace BenefitsStreamliner.Core.Models;

// Status codes stored in the database. The display names live in StatusNames.cs,
// so renaming a status for the UI is a one-line change there.
public enum ApplicationStatus
{
    Submitted,
    InReview,
    WaitingForHours,
    PendingFinanceReview,
    Approved,
    Denied,
    Transferred,
    Error
}

public class Application
{
    public int Id { get; set; }
    public string ApplicationId { get; set; } = string.Empty;   // e.g. APP-000001

    // Plan member
    public string EmployeeName { get; set; } = string.Empty;
    public string SinEncrypted { get; set; } = string.Empty;    // never stored in plain text
    public string SinLast3 { get; set; } = string.Empty;        // for display / lookups
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MailingAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;        // 2-letter code
    public string PostalCode { get; set; } = string.Empty;

    // Company
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyAddress { get; set; } = string.Empty;
    public string CompanyCity { get; set; } = string.Empty;
    public string CompanyProvince { get; set; } = string.Empty;
    public string CompanyPostalCode { get; set; } = string.Empty;

    // Training and claim
    public string Trade { get; set; } = string.Empty;
    public int TrainingYear { get; set; }                       // 1 to 4
    public DateTime TrainingFrom { get; set; }
    public DateTime TrainingTo { get; set; }                    // course completion date
    public DateTime ReturnedToWork { get; set; }
    public string PayTo { get; set; } = string.Empty;           // Employer | Employee
    public string CertificateNumber { get; set; } = string.Empty;
    public string TuitionCode { get; set; } = string.Empty;

    // Sign-off
    public string EmployeeSignature { get; set; } = string.Empty;
    public DateTime EmployeeSignDate { get; set; }
    public string RepName { get; set; } = string.Empty;
    public string RepPosition { get; set; } = string.Empty;
    public string RepSignature { get; set; } = string.Empty;
    public DateTime RepSignDate { get; set; }

    // Processing
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
    public DateTime? NextRecheckAt { get; set; }                // set while Waiting for Hours
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ApplicationDocument> Documents { get; set; } = new();
}

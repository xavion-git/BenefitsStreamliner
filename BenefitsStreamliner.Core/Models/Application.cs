namespace BenefitsStreamliner.Core.Models;

// Status codes stored in the database. Display names (the agreed names from the future-state flow)
// live in StatusNames.cs.
public enum ApplicationStatus
{
    New,
    MissingDocuments,   // shown on the form when validation fails; never stored
    Duplicate,
    Reviewing,
    WaitingForHours,
    PendingApproval,
    Approved,
    Denied,
    ChequePrinted,      // reserved for the Accounting phase; not used by the prototype yet
    Error               // technical failure (iBenefits down after all retries); not in the flow diagram
}

public class Application
{
    public int Id { get; set; }
    public string ApplicationId { get; set; } = string.Empty;   // e.g. APP-000001

    // 1. Employee
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string SinEncrypted { get; set; } = string.Empty;    // never stored in plain text
    public string SinLast3 { get; set; } = string.Empty;        // for display / lookups
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MailingAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;        // 2-letter code
    public string PostalCode { get; set; } = string.Empty;
    public string CertificateNumber { get; set; } = string.Empty;   // digits only, required

    // 2. Employer
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyAddress { get; set; } = string.Empty;
    public string CompanyCity { get; set; } = string.Empty;
    public string CompanyProvince { get; set; } = string.Empty;
    public string CompanyPostalCode { get; set; } = string.Empty;

    // 3. Training
    public string Trade { get; set; } = string.Empty;
    public int TrainingYear { get; set; }                       // 1 to 4
    public string TrainingProvince { get; set; } = string.Empty; // drives Proof of Payment (ON, BC) and the NB/NS exclusion
    public DateTime TrainingFrom { get; set; }
    public DateTime TrainingTo { get; set; }                    // course completion date
    public DateTime ReturnedToWork { get; set; }

    // 4. Refund payment
    public string PayTo { get; set; } = string.Empty;           // Employer | Employee

    // 6. Declaration
    public DateTime TermsAcceptedAt { get; set; }
    public string EmployeeSignature { get; set; } = string.Empty;
    public DateTime EmployeeSignDate { get; set; }
    public string RepName { get; set; } = string.Empty;
    public string RepPosition { get; set; } = string.Empty;
    public string RepSignature { get; set; } = string.Empty;
    public DateTime RepSignDate { get; set; }

    // Processing
    public ApplicationStatus Status { get; set; } = ApplicationStatus.New;
    public string? DuplicateOfApplicationId { get; set; }       // set when Status = Duplicate
    public DateTime? NextRecheckAt { get; set; }                // set while Waiting for Hours
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ApplicationDocument> Documents { get; set; } = new();
}

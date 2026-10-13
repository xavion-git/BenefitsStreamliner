namespace BenefitsStreamliner.Core.Models;

public static class DocumentTypes
{
    public const string MarksAndCompletion = "MarksAndProofOfCompletion";
    public const string ProofOfPayment = "ProofOfPayment";
}

public class ApplicationDocument
{
    public int Id { get; set; }
    public int ApplicationRowId { get; set; }                   // FK to Application.Id
    public string DocumentType { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;  // generated name on disk
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

namespace BenefitsStreamliner.Core.Models;

public enum ApplicationStatus { Submitted, Processing, Completed, Error }

public class Application
{
    public int Id { get; set; }
    public string ApplicationId { get; set; } = string.Empty;   // e.g. APP-000001
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public decimal Income { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
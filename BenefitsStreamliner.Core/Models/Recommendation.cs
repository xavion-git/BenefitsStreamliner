namespace BenefitsStreamliner.Core.Models;

public class Recommendation
{
    public int Id { get; set; }
    public string ApplicationId { get; set; } = string.Empty;
    // C# doesn't allow a member named like its class, so this is "Decision" in code
    // and mapped to the "Recommendation" column in the database.
    public string Decision { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
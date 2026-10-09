using System.ComponentModel.DataAnnotations;

namespace BenefitsStreamliner.Core.DTOs;

public class DecisionRequest
{
    [Required] public string Decision { get; set; } = string.Empty;   // "Approve" or "Reject"
}

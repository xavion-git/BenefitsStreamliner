namespace BenefitsStreamliner.Core.Models;

// Display names for statuses. "Waiting for Hours" and "Denied" come from the current-state doc.
// The others are working names: confirm them against the doc and rename here if they differ.
public static class StatusNames
{
    public static string Display(this ApplicationStatus status) => status switch
    {
        ApplicationStatus.Submitted => "Submitted",
        ApplicationStatus.InReview => "In Review",                          // TO CONFIRM
        ApplicationStatus.WaitingForHours => "Waiting for Hours",
        ApplicationStatus.PendingFinanceReview => "Pending Finance Review", // TO CONFIRM
        ApplicationStatus.Approved => "Approved",                           // TO CONFIRM
        ApplicationStatus.Denied => "Denied",
        ApplicationStatus.Transferred => "Transferred",                     // TO CONFIRM
        ApplicationStatus.Error => "Error",
        _ => status.ToString()
    };
}

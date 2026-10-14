namespace BenefitsStreamliner.Core.Models;

// Display names for statuses: the agreed names from the Tuition Refund future-state flow.
// "Error" is not in that flow; it covers technical failures only.
public static class StatusNames
{
    public static string Display(this ApplicationStatus status) => status switch
    {
        ApplicationStatus.New => "New",
        ApplicationStatus.MissingDocuments => "Missing Documents",
        ApplicationStatus.Duplicate => "Duplicate",
        ApplicationStatus.Reviewing => "Reviewing",
        ApplicationStatus.WaitingForHours => "Waiting for Hours",
        ApplicationStatus.PendingApproval => "Pending Approval",
        ApplicationStatus.Approved => "Approved",
        ApplicationStatus.Denied => "Denied",
        ApplicationStatus.ChequePrinted => "Cheque Printed",
        ApplicationStatus.Error => "Error",
        _ => status.ToString()
    };
}

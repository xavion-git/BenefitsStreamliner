using BenefitsStreamliner.Core.Models;

namespace BenefitsStreamliner.Tests;

public class StatusNamesTests
{
    // The agreed names from the Tuition Refund future-state flow.
    [Theory]
    [InlineData(ApplicationStatus.New, "New")]
    [InlineData(ApplicationStatus.MissingDocuments, "Missing Documents")]
    [InlineData(ApplicationStatus.Duplicate, "Duplicate")]
    [InlineData(ApplicationStatus.Reviewing, "Reviewing")]
    [InlineData(ApplicationStatus.WaitingForHours, "Waiting for Hours")]
    [InlineData(ApplicationStatus.PendingApproval, "Pending Approval")]
    [InlineData(ApplicationStatus.Approved, "Approved")]
    [InlineData(ApplicationStatus.Denied, "Denied")]
    [InlineData(ApplicationStatus.ChequePrinted, "Cheque Printed")]
    public void Statuses_use_the_agreed_names(ApplicationStatus status, string expected) =>
        Assert.Equal(expected, status.Display());
}

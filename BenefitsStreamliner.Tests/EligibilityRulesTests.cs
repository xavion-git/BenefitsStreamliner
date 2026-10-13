using BenefitsStreamliner.Core.DTOs;
using BenefitsStreamliner.Core.Models;
using BenefitsStreamliner.Core.Rules;

namespace BenefitsStreamliner.Tests;

public class EligibilityRulesTests
{
    private static readonly DateTime Today = new(2026, 10, 12);

    private static Application App(DateTime? trainingTo = null, DateTime? returned = null)
    {
        var to = trainingTo ?? Today.AddDays(-30);
        return new Application { TrainingFrom = to.AddDays(-30), TrainingTo = to, ReturnedToWork = returned ?? to.AddDays(3) };
    }

    private static BenefitsData Data(decimal hours = 200, string status = "Valid", int monthsOut = 0,
        bool onHire = false, bool member = true, bool fullTime = true, bool found = true) => new()
    {
        CertificateNumber = "CERT-1001", Found = found, BenefitStatus = status, HoursWorked = hours,
        MonthsOutOfBenefit = monthsOut, StartedTrainingOnHire = onHire, EmployerIsMember = member, FullTime = fullTime
    };

    private static EligibilityOutcome Run(BenefitsData d, Application? a = null) =>
        EligibilityRules.Evaluate(a ?? App(), d, Today).Outcome;

    [Fact] public void Eligible_at_exactly_150_hours() => Assert.Equal(EligibilityOutcome.Eligible, Run(Data(150)));
    [Fact] public void Waiting_for_hours_at_149() => Assert.Equal(EligibilityOutcome.WaitingForHours, Run(Data(149)));

    [Fact] public void Needs_300_when_out_of_benefit_8_months() =>
        Assert.Equal(EligibilityOutcome.WaitingForHours, Run(Data(299, monthsOut: 8)));
    [Fact] public void Eligible_at_300_when_out_of_benefit_8_months() =>
        Assert.Equal(EligibilityOutcome.Eligible, Run(Data(300, monthsOut: 8)));
    [Fact] public void Still_150_when_out_of_benefit_only_7_months() =>
        Assert.Equal(EligibilityOutcome.Eligible, Run(Data(150, monthsOut: 7)));
    [Fact] public void Needs_300_when_training_started_on_hire() =>
        Assert.Equal(EligibilityOutcome.WaitingForHours, Run(Data(250, onHire: true)));

    [Fact] public void Required_hours_follow_the_rule()
    {
        Assert.Equal(150m, EligibilityRules.RequiredHours(Data()));
        Assert.Equal(300m, EligibilityRules.RequiredHours(Data(monthsOut: 9)));
        Assert.Equal(300m, EligibilityRules.RequiredHours(Data(onHire: true)));
    }

    [Fact] public void Denied_when_benefit_status_is_not_valid() => Assert.Equal(EligibilityOutcome.Denied, Run(Data(400, status: "Terminated")));
    [Fact] public void Denied_when_employer_is_not_a_member() => Assert.Equal(EligibilityOutcome.Denied, Run(Data(400, member: false)));
    [Fact] public void Denied_when_not_back_at_full_time_work() => Assert.Equal(EligibilityOutcome.Denied, Run(Data(400, fullTime: false)));
    [Fact] public void Denied_when_certificate_not_found() => Assert.Equal(EligibilityOutcome.Denied, Run(Data(found: false)));
    [Fact] public void Denied_when_return_to_work_date_is_in_the_future() =>
        Assert.Equal(EligibilityOutcome.Denied, Run(Data(400), App(returned: Today.AddDays(5))));

    [Fact] public void Still_waiting_on_the_last_day_of_the_6_month_window() =>
        Assert.Equal(EligibilityOutcome.WaitingForHours, Run(Data(100), App(trainingTo: Today.AddMonths(-6))));
    [Fact] public void Denied_the_day_after_the_6_month_window_closes() =>
        Assert.Equal(EligibilityOutcome.Denied, Run(Data(100), App(trainingTo: Today.AddMonths(-6).AddDays(-1))));
    [Fact] public void Hours_that_are_met_win_over_the_deadline() =>
        Assert.Equal(EligibilityOutcome.Eligible, Run(Data(150), App(trainingTo: Today.AddMonths(-8))));
}

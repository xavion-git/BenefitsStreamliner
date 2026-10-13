using BenefitsStreamliner.Core.DTOs;
using BenefitsStreamliner.Core.Models;

namespace BenefitsStreamliner.Core.Rules;

public enum EligibilityOutcome { Eligible, WaitingForHours, Denied }

public record EligibilityResult(EligibilityOutcome Outcome, string Reason);

// The real tuition refund rules (replaces the placeholder income / age / DOB rules).
public static class EligibilityRules
{
    public const decimal StandardHours = 150;
    public const decimal ExtendedHours = 300;
    public const int ExtendedOutOfBenefitMonths = 8;
    public const int HoursWindowMonths = 6;

    // 150 hours, or 300 if the plan member was out of benefit 8+ months or started training right on hire.
    public static decimal RequiredHours(BenefitsData benefits) =>
        benefits.MonthsOutOfBenefit >= ExtendedOutOfBenefitMonths || benefits.StartedTrainingOnHire
            ? ExtendedHours
            : StandardHours;

    // Hours must be in within 6 months of course completion (the training end date).
    public static DateTime HoursDeadline(Application app) =>
        app.TrainingTo.Date.AddMonths(HoursWindowMonths);

    public static EligibilityResult Evaluate(Application app, BenefitsData benefits, DateTime today)
    {
        if (!benefits.Found)
            return new(EligibilityOutcome.Denied, "Certificate number was not found in iBenefits.");

        // 1. Benefit status must be valid.
        if (!string.Equals(benefits.BenefitStatus, "Valid", StringComparison.OrdinalIgnoreCase))
            return new(EligibilityOutcome.Denied, $"Benefit status is \"{benefits.BenefitStatus}\"; it must be Valid.");

        // 2. Return to full-time work at an OpenCircle member company.
        if (!benefits.EmployerIsMember)
            return new(EligibilityOutcome.Denied, "Employer is not an OpenCircle member company.");
        if (!benefits.FullTime)
            return new(EligibilityOutcome.Denied, "Plan member has not returned to full-time work.");
        if (app.ReturnedToWork.Date > today.Date)
            return new(EligibilityOutcome.Denied, "The return-to-work date is in the future.");

        // 3. Hours.
        var required = RequiredHours(benefits);
        var deadline = HoursDeadline(app);
        var hours = $"{benefits.HoursWorked:0.##} hours worked, {required:0} required";

        if (benefits.HoursWorked >= required)
            return new(EligibilityOutcome.Eligible, $"Meets the hours requirement: {hours}.");

        if (today.Date > deadline)
            return new(EligibilityOutcome.Denied,
                $"Hours were not received within 6 months of course completion (deadline {deadline:yyyy-MM-dd}): {hours}.");

        return new(EligibilityOutcome.WaitingForHours,
            $"{hours}. Hours are re-checked monthly until {deadline:yyyy-MM-dd}.");
    }
}

using BenefitsStreamliner.Core.DTOs;
using BenefitsStreamliner.Core.Models;

namespace BenefitsStreamliner.Core;

public static class RecommendationLogic
{
    public static (string Decision, string Reason) Evaluate(Application app, BenefitsData benefits, DateTime today)
    {
        var age = today.Year - app.DateOfBirth.Year;
        if (app.DateOfBirth.Date > today.Date.AddYears(-age)) age--;

        if (!benefits.Eligible)
            return ("Not Eligible", "iBenefits reports no active benefit program for this applicant.");
        if (age < benefits.MinimumAge)
            return ("Not Eligible", $"Applicant is {age}; the minimum age is {benefits.MinimumAge}.");
        if (app.Income > benefits.IncomeLimit)
            return ("Not Eligible", $"Income {app.Income:C0} exceeds the limit of {benefits.IncomeLimit:C0}.");

        return ("Eligible", $"Applicant meets the criteria. Estimated benefit: {benefits.BenefitAmount:C0}.");
    }
}
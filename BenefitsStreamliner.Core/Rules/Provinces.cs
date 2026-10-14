namespace BenefitsStreamliner.Core.Rules;

public record ProvinceOption(string Code, string Name);

public static class Provinces
{
    // Every province and territory, as listed on the claim form.
    public static readonly IReadOnlyList<ProvinceOption> All = new[]
    {
        new ProvinceOption("AB", "Alberta"),
        new ProvinceOption("BC", "British Columbia"),
        new ProvinceOption("MB", "Manitoba"),
        new ProvinceOption("NB", "New Brunswick"),
        new ProvinceOption("NL", "Newfoundland and Labrador"),
        new ProvinceOption("NS", "Nova Scotia"),
        new ProvinceOption("NT", "Northwest Territories"),
        new ProvinceOption("NU", "Nunavut"),
        new ProvinceOption("ON", "Ontario"),
        new ProvinceOption("PE", "Prince Edward Island"),
        new ProvinceOption("QC", "Quebec"),
        new ProvinceOption("SK", "Saskatchewan"),
        new ProvinceOption("YT", "Yukon"),
    };

    // Training completed in these provinces is not handled by the program.
    public static readonly string[] Excluded = { "NB", "NS" };

    public static bool IsValid(string? code) =>
        All.Any(p => string.Equals(p.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static bool IsExcluded(string? code) =>
        Excluded.Contains(code?.Trim(), StringComparer.OrdinalIgnoreCase);

    // Ontario and British Columbia: receipts determine the refund, so Proof of Payment is required.
    public static bool RequiresProofOfPayment(string? code) =>
        string.Equals(code?.Trim(), "ON", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(code?.Trim(), "BC", StringComparison.OrdinalIgnoreCase);
}

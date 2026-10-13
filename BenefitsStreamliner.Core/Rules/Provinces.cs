namespace BenefitsStreamliner.Core.Rules;

public record ProvinceOption(string Code, string Name);

public static class Provinces
{
    // Dropdown contents. New Brunswick and Nova Scotia are excluded from the program.
    public static readonly IReadOnlyList<ProvinceOption> Selectable = new[]
    {
        new ProvinceOption("AB", "Alberta"),
        new ProvinceOption("BC", "British Columbia"),
        new ProvinceOption("MB", "Manitoba"),
        new ProvinceOption("NL", "Newfoundland and Labrador"),
        new ProvinceOption("NT", "Northwest Territories"),
        new ProvinceOption("NU", "Nunavut"),
        new ProvinceOption("ON", "Ontario"),
        new ProvinceOption("PE", "Prince Edward Island"),
        new ProvinceOption("QC", "Quebec"),
        new ProvinceOption("SK", "Saskatchewan"),
        new ProvinceOption("YT", "Yukon"),
    };

    public static readonly string[] Excluded = { "NB", "NS" };

    public static bool IsExcluded(string? code) =>
        Excluded.Contains(code?.Trim(), StringComparer.OrdinalIgnoreCase);

    public static bool IsSelectable(string? code) =>
        Selectable.Any(p => string.Equals(p.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase));

    // Ontario and British Columbia claims must include a Proof of Payment.
    public static bool RequiresProofOfPayment(string? code) =>
        string.Equals(code?.Trim(), "ON", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(code?.Trim(), "BC", StringComparison.OrdinalIgnoreCase);
}

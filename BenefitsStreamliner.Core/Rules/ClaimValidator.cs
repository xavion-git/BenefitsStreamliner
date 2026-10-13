using System.Text.RegularExpressions;
using BenefitsStreamliner.Core.DTOs;

namespace BenefitsStreamliner.Core.Rules;

public record FileMeta(string Name, long Size);

// One set of validation rules, used by the API (authoritative) and the Blazor form (instant feedback).
// Returns a dictionary of field name -> message. Empty means valid.
public static class ClaimValidator
{
    public const long MaxFileBytes = 10 * 1024 * 1024;
    public const int MaxDocuments = 5;

    private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
    private static readonly Regex PostalPattern = new(@"^[A-Za-z]\d[A-Za-z][ -]?\d[A-Za-z]\d$", RegexOptions.Compiled);
    private static readonly Regex EmailPattern = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

    // Proof of Payment is required when the province is ON or BC.
    // This uses the plan member's (mailing) province. Change it here if the rule should key off the company province.
    public static bool ProofOfPaymentRequired(ApplicationRequest r) =>
        Provinces.RequiresProofOfPayment(r.Province);

    public static Dictionary<string, string> Validate(
        ApplicationRequest r, IReadOnlyList<FileMeta> documents, FileMeta? proofOfPayment)
    {
        var e = new Dictionary<string, string>();

        void Text(string key, string? value, string label, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) e[key] = $"{label} is required.";
            else if (value.Trim().Length > max) e[key] = $"{label} must be {max} characters or fewer.";
        }
        void Date(string key, DateTime? value, string label)
        {
            if (value is null) e[key] = $"{label} is required.";
        }
        void Province(string key, string? value, string label)
        {
            if (string.IsNullOrWhiteSpace(value)) e[key] = $"{label} is required.";
            else if (Provinces.IsExcluded(value)) e[key] = $"{label}: claims from New Brunswick and Nova Scotia are not accepted.";
            else if (!Provinces.IsSelectable(value)) e[key] = $"Select a {label.ToLowerInvariant()} from the list.";
        }
        void Postal(string key, string? value, string label)
        {
            if (string.IsNullOrWhiteSpace(value)) e[key] = $"{label} is required.";
            else if (!PostalPattern.IsMatch(value.Trim())) e[key] = "Enter a valid postal code (e.g. T5L 4H5).";
        }

        Text("EmployeeName", r.EmployeeName, "Employee name", 100);
        Text("CertificateNumber", r.CertificateNumber, "Certificate / member number", 30);
        Text("TuitionCode", r.TuitionCode, "Tuition code", 20);

        // SIN: 9 digits and a valid Luhn checksum.
        var sin = SinValidator.Digits(r.Sin);
        if (sin.Length == 0) e["Sin"] = "Social Insurance Number is required.";
        else if (sin.Length != 9) e["Sin"] = "Social Insurance Number must be 9 digits (3 digits in each box).";
        else if (!SinValidator.IsValid(sin)) e["Sin"] = "This Social Insurance Number is not valid. Check the digits and try again.";

        if (string.IsNullOrWhiteSpace(r.Phone)) e["Phone"] = "Phone number is required.";
        else if (r.Phone.Count(char.IsDigit) < 10) e["Phone"] = "Enter a valid phone number with area code.";
        else if (r.Phone.Trim().Length > 30) e["Phone"] = "Phone number must be 30 characters or fewer.";

        if (string.IsNullOrWhiteSpace(r.Email)) e["Email"] = "Email is required.";
        else if (!EmailPattern.IsMatch(r.Email.Trim()) || r.Email.Trim().Length > 150) e["Email"] = "Enter a valid email address.";

        Text("MailingAddress", r.MailingAddress, "Mailing address", 200);
        Text("City", r.City, "City", 100);
        Province("Province", r.Province, "Province");
        Postal("PostalCode", r.PostalCode, "Postal code");

        Text("CompanyName", r.CompanyName, "Company name", 150);
        Text("CompanyAddress", r.CompanyAddress, "Company address", 200);
        Text("CompanyCity", r.CompanyCity, "Company city", 100);
        Province("CompanyProvince", r.CompanyProvince, "Company province");
        Postal("CompanyPostalCode", r.CompanyPostalCode, "Company postal code");

        Text("Trade", r.Trade, "Trade or occupation training", 100);
        if (r.TrainingYear is null or < 1 or > 4) e["TrainingYear"] = "Select a training year.";

        Date("TrainingFrom", r.TrainingFrom, "Training start date");
        Date("TrainingTo", r.TrainingTo, "Training end date");
        Date("ReturnedToWork", r.ReturnedToWork, "Return to work date");
        if (r.TrainingFrom is not null && r.TrainingTo is not null && r.TrainingTo.Value.Date < r.TrainingFrom.Value.Date)
            e["TrainingTo"] = "End date cannot be before the start date.";

        if (r.PayTo is not ("Employer" or "Employee"))
            e["PayTo"] = "Select who the refund is to be paid to (Employer or Employee).";

        Text("EmployeeSignature", r.EmployeeSignature, "Employee signature", 100);
        Date("EmployeeSignDate", r.EmployeeSignDate, "Employee signature date");
        Text("RepName", r.RepName, "Employer representative name", 100);
        Text("RepPosition", r.RepPosition, "Position", 100);
        Text("RepSignature", r.RepSignature, "Representative signature", 100);
        Date("RepSignDate", r.RepSignDate, "Representative signature date");

        // Marks and Proof of Completion
        if (documents.Count == 0)
            e["Documents"] = "Please upload your Marks and Proof of Completion.";
        else if (documents.Count > MaxDocuments)
            e["Documents"] = $"Upload no more than {MaxDocuments} documents.";
        else
        {
            var bad = documents.Where(f => !IsAllowed(f)).Select(f => f.Name).ToList();
            if (bad.Count > 0)
                e["Documents"] = "These files are not allowed, are empty, or exceed 10 MB: " + string.Join(", ", bad);
        }

        // Proof of Payment (ON and BC)
        if (proofOfPayment is null)
        {
            if (ProofOfPaymentRequired(r))
                e["ProofOfPayment"] = "Proof of Payment is required for Ontario and British Columbia.";
        }
        else if (!IsAllowed(proofOfPayment))
        {
            e["ProofOfPayment"] = "Proof of Payment must be a PDF, JPG, PNG, DOC or DOCX file up to 10 MB.";
        }

        return e;
    }

    private static bool IsAllowed(FileMeta f) =>
        AllowedExtensions.Contains(Path.GetExtension(f.Name).ToLowerInvariant()) && f.Size > 0 && f.Size <= MaxFileBytes;
}

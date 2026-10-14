using System.Text.RegularExpressions;
using BenefitsStreamliner.Core.DTOs;

namespace BenefitsStreamliner.Core.Rules;

public record FileMeta(string Name, long Size);

// One set of validation rules, used by the API (authoritative) and the Blazor form (instant feedback).
// Returns a dictionary of field name -> message. Empty means valid.
public static class ClaimValidator
{
    public const long MaxFileBytes = 10 * 1024 * 1024;
    public const int MaxFiles = 10;                       // across both upload boxes

    private static readonly Regex NamePattern = new(@"^\p{L}[\p{L}' .-]*$", RegexOptions.Compiled);
    private static readonly Regex PostalPattern = new(@"^[ABCEGHJ-NPRSTVXY]\d[ABCEGHJ-NPRSTV-Z] ?\d[ABCEGHJ-NPRSTV-Z]\d$", RegexOptions.Compiled);
    private static readonly Regex EmailPattern = new(@"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$", RegexOptions.Compiled);
    private static readonly Regex CertificatePattern = new(@"^\d{1,15}$", RegexOptions.Compiled);
    private static readonly Regex FilePattern = new(@"\.(pdf|jpe?g|png|docx?)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);

    // Proof of Payment is required when the training was completed in Ontario or British Columbia.
    public static bool ProofOfPaymentRequired(ApplicationRequest r) =>
        Provinces.RequiresProofOfPayment(r.TrainingProvince);

    public static Dictionary<string, string> Validate(
        ApplicationRequest r,
        IReadOnlyList<FileMeta> completionDocs,
        IReadOnlyList<FileMeta> paymentDocs,
        DateTime today)
    {
        var e = new Dictionary<string, string>();
        today = today.Date;

        void Text(string key, string? value, string label, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) e[key] = $"{label} is required.";
            else if (value.Trim().Length > max) e[key] = $"{label} must be {max} characters or fewer.";
        }
        void PersonName(string key, string? value, string label)
        {
            Text(key, value, label, 60);
            if (!e.ContainsKey(key) && !NamePattern.IsMatch(value!.Trim()))
                e[key] = "Use letters, spaces, hyphens or apostrophes only.";
        }
        void Province(string key, string? value, string label)
        {
            if (string.IsNullOrWhiteSpace(value)) e[key] = $"{label} is required.";
            else if (!Provinces.IsValid(value)) e[key] = $"Select a valid {label.ToLowerInvariant()}.";
        }
        void Postal(string key, string? value, string label)
        {
            if (string.IsNullOrWhiteSpace(value)) e[key] = $"{label} is required.";
            else if (!PostalPattern.IsMatch(value.Trim().ToUpperInvariant())) e[key] = "Enter a valid postal code, e.g. T5L 4H5.";
        }
        DateTime? PastDate(string key, DateTime? value, string label, string futureMessage)
        {
            if (value is null) { e[key] = $"{label} is required."; return null; }
            if (value.Value.Date > today) { e[key] = futureMessage; return null; }
            return value.Value.Date;
        }

        // 1. Employee
        PersonName("FirstName", r.FirstName, "First name");
        PersonName("LastName", r.LastName, "Last name");

        // SIN: 9 digits and a valid Luhn checksum.
        var sin = SinValidator.Digits(r.Sin);
        if (sin.Length == 0) e["Sin"] = "Social Insurance Number is required.";
        else if (sin.Length != 9) e["Sin"] = "Enter all 9 digits of your Social Insurance Number.";
        else if (!SinValidator.IsValid(sin)) e["Sin"] = "This is not a valid Social Insurance Number. Please check the digits.";

        var phone = SinValidator.Digits(r.Phone);
        if (phone.Length == 11 && phone[0] == '1') phone = phone[1..];
        if (string.IsNullOrWhiteSpace(r.Phone)) e["Phone"] = "Phone is required.";
        else if (phone.Length != 10 || r.Phone.Trim().Length > 20) e["Phone"] = "Enter a 10-digit phone number, including the area code.";

        if (string.IsNullOrWhiteSpace(r.Email)) e["Email"] = "Email is required.";
        else if (r.Email.Trim().Length > 254 || !EmailPattern.IsMatch(r.Email.Trim())) e["Email"] = "Enter a valid email address, e.g. name@example.com.";

        Text("MailingAddress", r.MailingAddress, "Mailing address", 120);
        Text("City", r.City, "City", 60);
        Province("Province", r.Province, "Province");
        Postal("PostalCode", r.PostalCode, "Postal code");

        // Required to submit. Digits only, up to 15.
        if (string.IsNullOrWhiteSpace(r.CertificateNumber)) e["CertificateNumber"] = "Certificate number is required.";
        else if (!CertificatePattern.IsMatch(r.CertificateNumber.Trim())) e["CertificateNumber"] = "Use digits only (up to 15).";

        // 2. Employer
        Text("CompanyName", r.CompanyName, "Company name", 120);
        Text("CompanyAddress", r.CompanyAddress, "Company address", 120);
        Text("CompanyCity", r.CompanyCity, "Company city", 60);
        Province("CompanyProvince", r.CompanyProvince, "Company province");
        Postal("CompanyPostalCode", r.CompanyPostalCode, "Company postal code");

        // 3. Training
        Text("Trade", r.Trade, "Trade or occupation training", 80);
        if (r.TrainingYear is null or < 1 or > 4) e["TrainingYear"] = "Select your training year.";

        if (string.IsNullOrWhiteSpace(r.TrainingProvince)) e["TrainingProvince"] = "Province of training is required.";
        else if (!Provinces.IsValid(r.TrainingProvince)) e["TrainingProvince"] = "Select a valid province of training.";
        else if (Provinces.IsExcluded(r.TrainingProvince))
            e["TrainingProvince"] = "Training completed in New Brunswick or Nova Scotia is not covered by the Tuition Refund Program.";

        var from = PastDate("TrainingFrom", r.TrainingFrom, "Training start date", "Training start date cannot be in the future.");
        var to = PastDate("TrainingTo", r.TrainingTo, "Training end date", "Training must be completed before you submit a claim.");
        var back = PastDate("ReturnedToWork", r.ReturnedToWork, "Return-to-work date", "Return-to-work date cannot be in the future.");
        if (from is not null && to is not null && to < from) e["TrainingTo"] = "Training end date cannot be before the start date.";
        if (to is not null && back is not null && back < to) e["ReturnedToWork"] = "Return-to-work date cannot be before the training end date.";

        // 4. Refund payment
        if (r.PayTo is not ("Employer" or "Employee")) e["PayTo"] = "Select who the refund is to be paid to.";

        // 5. Supporting documents
        var payNeeded = ProofOfPaymentRequired(r);
        var payment = payNeeded ? paymentDocs : Array.Empty<FileMeta>();

        if (completionDocs.Count == 0) e["CompletionDocs"] = "Upload your Marks and Proof of Completion.";
        if (payNeeded && paymentDocs.Count == 0) e["PaymentDocs"] = "Upload your Proof of Payment.";

        var badCompletion = completionDocs.Where(f => !IsAllowed(f)).Select(f => f.Name).ToList();
        if (badCompletion.Count > 0)
            e["CompletionDocs"] = "Not accepted (wrong type, empty, or over 10 MB): " + string.Join(", ", badCompletion);
        var badPayment = payment.Where(f => !IsAllowed(f)).Select(f => f.Name).ToList();
        if (badPayment.Count > 0)
            e["PaymentDocs"] = "Not accepted (wrong type, empty, or over 10 MB): " + string.Join(", ", badPayment);
        if (completionDocs.Count + payment.Count > MaxFiles)
            e["CompletionDocs"] = $"Upload no more than {MaxFiles} files in total.";

        // 6. Declaration and signatures
        if (r.AcceptTerms != true) e["AcceptTerms"] = "You must accept the terms and conditions to submit.";

        Text("EmployeeSignature", r.EmployeeSignature, "Employee signature", 120);
        var fullName = Normalize($"{r.FirstName} {r.LastName}");
        if (!e.ContainsKey("EmployeeSignature") && !e.ContainsKey("FirstName") && !e.ContainsKey("LastName") &&
            Normalize(r.EmployeeSignature) != fullName)
            e["EmployeeSignature"] = $"Your signature must match the name above ({r.FirstName?.Trim()} {r.LastName?.Trim()}).";
        PastDate("EmployeeSignDate", r.EmployeeSignDate, "Employee signature date", "Signature date cannot be in the future.");

        Text("RepName", r.RepName, "Employer representative name", 120);
        Text("RepPosition", r.RepPosition, "Position", 80);
        Text("RepSignature", r.RepSignature, "Representative signature", 120);
        if (!e.ContainsKey("RepSignature") && !e.ContainsKey("RepName") &&
            Normalize(r.RepSignature) != Normalize(r.RepName))
            e["RepSignature"] = "The representative signature must match the representative name.";
        PastDate("RepSignDate", r.RepSignDate, "Representative signature date", "Signature date cannot be in the future.");

        return e;
    }

    private static string Normalize(string? value) => Spaces.Replace(value ?? string.Empty, " ").Trim().ToLowerInvariant();

    private static bool IsAllowed(FileMeta f) =>
        FilePattern.IsMatch(f.Name) && f.Size > 0 && f.Size <= MaxFileBytes;
}

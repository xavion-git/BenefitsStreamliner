using BenefitsStreamliner.Core.DTOs;
using BenefitsStreamliner.Core.Rules;

namespace BenefitsStreamliner.Tests;

public class ClaimValidatorTests
{
    private static readonly DateTime Today = new(2026, 10, 12);
    private static readonly FileMeta[] Marks = { new("ait-letter.pdf", 1000) };
    private static readonly FileMeta[] Receipts = { new("receipt.pdf", 1000) };
    private static readonly FileMeta[] None = Array.Empty<FileMeta>();

    private static ApplicationRequest Valid(string trainingProvince = "AB") => new()
    {
        FirstName = "Jane", LastName = "Doe", Sin = "046454286", Phone = "(780) 555-1234", Email = "jane@example.com",
        MailingAddress = "1 Main St", City = "Edmonton", Province = "AB", PostalCode = "T5L 4H5",
        CertificateNumber = "1001",
        CompanyName = "ABC Ltd", CompanyAddress = "2 Side St", CompanyCity = "Edmonton", CompanyProvince = "AB", CompanyPostalCode = "T5L 4H5",
        Trade = "Electrician", TrainingYear = 2, TrainingProvince = trainingProvince,
        TrainingFrom = new DateTime(2026, 8, 3), TrainingTo = new DateTime(2026, 9, 11), ReturnedToWork = new DateTime(2026, 9, 14),
        PayTo = "Employee", AcceptTerms = true,
        EmployeeSignature = "Jane Doe", EmployeeSignDate = new DateTime(2026, 10, 1),
        RepName = "Bob Lee", RepPosition = "Manager", RepSignature = "Bob Lee", RepSignDate = new DateTime(2026, 10, 2)
    };

    private static Dictionary<string, string> Check(ApplicationRequest r, FileMeta[]? completion = null, FileMeta[]? payment = null) =>
        ClaimValidator.Validate(r, completion ?? Marks, payment ?? None, Today);

    [Fact] public void A_complete_alberta_claim_is_valid() => Assert.Empty(Check(Valid()));

    // ----- certificate number: required, digits only -----
    [Fact] public void Certificate_number_is_required()
    {
        var r = Valid(); r.CertificateNumber = "  ";
        Assert.True(Check(r).ContainsKey("CertificateNumber"));
        r.CertificateNumber = null;
        Assert.True(Check(r).ContainsKey("CertificateNumber"));
    }

    [Theory]
    [InlineData("CERT-1001")]
    [InlineData("12 34")]
    [InlineData("1234567890123456")]   // 16 digits
    public void Certificate_number_must_be_1_to_15_digits(string value)
    {
        var r = Valid(); r.CertificateNumber = value;
        Assert.True(Check(r).ContainsKey("CertificateNumber"));
    }

    [Fact] public void A_15_digit_certificate_number_is_accepted()
    {
        var r = Valid(); r.CertificateNumber = "123456789012345";
        Assert.Empty(Check(r));
    }

    // ----- training province: Proof of Payment and the NB/NS exclusion -----
    [Theory]
    [InlineData("ON")]
    [InlineData("BC")]
    public void Proof_of_payment_is_required_when_training_was_in_ON_or_BC(string province)
    {
        var r = Valid(province);
        Assert.True(ClaimValidator.ProofOfPaymentRequired(r));
        Assert.True(Check(r).ContainsKey("PaymentDocs"));
        Assert.Empty(Check(r, payment: Receipts));
    }

    [Fact] public void Proof_of_payment_is_not_required_elsewhere()
    {
        Assert.False(ClaimValidator.ProofOfPaymentRequired(Valid("AB")));
        Assert.False(Check(Valid("SK")).ContainsKey("PaymentDocs"));
    }

    [Fact] public void Mailing_province_does_not_trigger_proof_of_payment()
    {
        var r = Valid("AB"); r.Province = "ON";            // lives in Ontario, trained in Alberta
        Assert.False(ClaimValidator.ProofOfPaymentRequired(r));
        Assert.Empty(Check(r));
    }

    [Theory]
    [InlineData("NB")]
    [InlineData("NS")]
    public void Training_in_NB_or_NS_is_not_covered(string province) =>
        Assert.True(Check(Valid(province)).ContainsKey("TrainingProvince"));

    [Fact] public void Living_in_NB_or_working_in_NS_does_not_block_a_claim()
    {
        var r = Valid(); r.Province = "NB"; r.CompanyProvince = "NS";
        Assert.Empty(Check(r));
    }

    [Fact] public void Province_list_has_all_13_and_flags_NB_NS_as_excluded()
    {
        Assert.Equal(13, Provinces.All.Count);
        Assert.True(Provinces.IsExcluded("NB"));
        Assert.True(Provinces.IsExcluded("ns"));
        Assert.False(Provinces.IsExcluded("ON"));
    }

    // ----- SIN, names, contact details -----
    [Fact] public void A_sin_that_fails_luhn_is_rejected()
    {
        var r = Valid(); r.Sin = "046454287";
        Assert.True(Check(r).ContainsKey("Sin"));
    }

    [Theory]
    [InlineData("J4ne 2")]
    [InlineData("")]
    public void First_name_must_be_letters(string name)
    {
        var r = Valid(); r.FirstName = name; r.EmployeeSignature = "x";
        Assert.True(Check(r).ContainsKey("FirstName"));
    }

    [Fact] public void Hyphens_and_apostrophes_are_fine_in_names()
    {
        var r = Valid(); r.LastName = "O'Brien-Smith"; r.EmployeeSignature = "Jane O'Brien-Smith";
        Assert.Empty(Check(r));
    }

    [Theory]
    [InlineData("780555123")]      // 9 digits
    [InlineData("78055512345")]    // 11 digits not starting with 1
    public void Phone_must_be_10_digits(string phone)
    {
        var r = Valid(); r.Phone = phone;
        Assert.True(Check(r).ContainsKey("Phone"));
    }

    [Fact] public void Eleven_digit_phone_starting_with_1_is_accepted()
    {
        var r = Valid(); r.Phone = "1-780-555-1234";
        Assert.Empty(Check(r));
    }

    [Theory]
    [InlineData("D5L 4H5")]     // D is not used as a first letter
    [InlineData("T5L4H")]
    public void Postal_code_must_be_a_real_canadian_format(string postal)
    {
        var r = Valid(); r.PostalCode = postal;
        Assert.True(Check(r).ContainsKey("PostalCode"));
    }

    [Fact] public void Lower_case_postal_code_without_a_space_is_accepted()
    {
        var r = Valid(); r.PostalCode = "t5l4h5";
        Assert.Empty(Check(r));
    }

    [Fact] public void Email_must_look_like_an_email()
    {
        var r = Valid(); r.Email = "jane@example";
        Assert.True(Check(r).ContainsKey("Email"));
    }

    // ----- dates -----
    [Fact] public void End_date_cannot_be_before_start_date()
    {
        var r = Valid(); r.TrainingTo = new DateTime(2026, 7, 1);
        Assert.True(Check(r).ContainsKey("TrainingTo"));
    }

    [Fact] public void Training_must_be_finished_before_submitting()
    {
        var r = Valid(); r.TrainingTo = Today.AddDays(1);
        Assert.True(Check(r).ContainsKey("TrainingTo"));
    }

    [Fact] public void Return_to_work_cannot_be_before_training_ended()
    {
        var r = Valid(); r.ReturnedToWork = new DateTime(2026, 9, 1);
        Assert.True(Check(r).ContainsKey("ReturnedToWork"));
    }

    [Fact] public void Return_to_work_and_signature_dates_cannot_be_in_the_future()
    {
        var r = Valid(); r.ReturnedToWork = Today.AddDays(2); r.EmployeeSignDate = Today.AddDays(1); r.RepSignDate = Today.AddDays(1);
        var errors = Check(r);
        Assert.True(errors.ContainsKey("ReturnedToWork"));
        Assert.True(errors.ContainsKey("EmployeeSignDate"));
        Assert.True(errors.ContainsKey("RepSignDate"));
    }

    // ----- declaration -----
    [Fact] public void Terms_must_be_accepted()
    {
        var r = Valid(); r.AcceptTerms = false;
        Assert.True(Check(r).ContainsKey("AcceptTerms"));
        r.AcceptTerms = null;
        Assert.True(Check(r).ContainsKey("AcceptTerms"));
    }

    [Fact] public void Employee_signature_must_match_the_name()
    {
        var r = Valid(); r.EmployeeSignature = "J. Doe";
        Assert.True(Check(r).ContainsKey("EmployeeSignature"));
        r.EmployeeSignature = "  jane   DOE ";             // spacing and case don't matter
        Assert.Empty(Check(r));
    }

    [Fact] public void Representative_signature_must_match_the_representative_name()
    {
        var r = Valid(); r.RepSignature = "Robert Lee";
        Assert.True(Check(r).ContainsKey("RepSignature"));
    }

    // ----- uploads -----
    [Fact] public void Marks_and_proof_of_completion_are_required() =>
        Assert.True(Check(Valid(), completion: None).ContainsKey("CompletionDocs"));

    [Fact] public void Disallowed_empty_or_oversized_files_are_rejected()
    {
        Assert.True(Check(Valid(), completion: new[] { new FileMeta("virus.exe", 100) }).ContainsKey("CompletionDocs"));
        Assert.True(Check(Valid(), completion: new[] { new FileMeta("empty.pdf", 0) }).ContainsKey("CompletionDocs"));
        Assert.True(Check(Valid(), completion: new[] { new FileMeta("big.pdf", ClaimValidator.MaxFileBytes + 1) }).ContainsKey("CompletionDocs"));
    }

    [Fact] public void No_more_than_10_files_in_total()
    {
        var six = Enumerable.Range(1, 6).Select(i => new FileMeta($"a{i}.pdf", 10)).ToArray();
        var five = Enumerable.Range(1, 5).Select(i => new FileMeta($"b{i}.pdf", 10)).ToArray();
        Assert.True(Check(Valid("ON"), completion: six, payment: five).ContainsKey("CompletionDocs"));   // 11 files
        Assert.Empty(Check(Valid("ON"), completion: five, payment: five));                              // 10 files
    }

    [Fact] public void Payment_files_are_ignored_when_not_needed()
    {
        // A bad payment file doesn't matter if the training province doesn't need Proof of Payment.
        Assert.Empty(Check(Valid("AB"), payment: new[] { new FileMeta("virus.exe", 100) }));
    }
}

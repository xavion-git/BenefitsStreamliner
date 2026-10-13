using BenefitsStreamliner.Core.DTOs;
using BenefitsStreamliner.Core.Rules;

namespace BenefitsStreamliner.Tests;

public class ClaimValidatorTests
{
    private static readonly FileMeta[] Docs = { new("marks.pdf", 1000) };
    private static readonly FileMeta Pop = new("payment.pdf", 1000);

    private static ApplicationRequest Valid(string province = "AB") => new()
    {
        EmployeeName = "Jane Doe", Sin = "046454286", Phone = "780-555-1234", Email = "jane@example.com",
        MailingAddress = "1 Main St", City = "Edmonton", Province = province, PostalCode = "T5L 4H5",
        CompanyName = "ABC Ltd", CompanyAddress = "2 Side St", CompanyCity = "Edmonton", CompanyProvince = "AB",
        CompanyPostalCode = "T5L 4H5", Trade = "Electrician", TrainingYear = 2,
        TrainingFrom = new DateTime(2026, 1, 5), TrainingTo = new DateTime(2026, 2, 13), ReturnedToWork = new DateTime(2026, 2, 16),
        PayTo = "Employee", CertificateNumber = "CERT-1001", TuitionCode = "TC-01",
        EmployeeSignature = "Jane Doe", EmployeeSignDate = new DateTime(2026, 3, 1),
        RepName = "Bob Lee", RepPosition = "Manager", RepSignature = "Bob Lee", RepSignDate = new DateTime(2026, 3, 2)
    };

    [Fact] public void A_complete_alberta_claim_is_valid() =>
        Assert.Empty(ClaimValidator.Validate(Valid(), Docs, null));

    [Theory]
    [InlineData("ON")]
    [InlineData("BC")]
    public void Proof_of_payment_is_required_for_ON_and_BC(string province)
    {
        var request = Valid(province);
        Assert.True(ClaimValidator.ProofOfPaymentRequired(request));
        Assert.True(ClaimValidator.Validate(request, Docs, null).ContainsKey("ProofOfPayment"));
        Assert.Empty(ClaimValidator.Validate(request, Docs, Pop));
    }

    [Fact] public void Proof_of_payment_is_not_required_elsewhere()
    {
        Assert.False(ClaimValidator.ProofOfPaymentRequired(Valid("AB")));
        Assert.False(ClaimValidator.Validate(Valid("SK"), Docs, null).ContainsKey("ProofOfPayment"));
    }

    [Theory]
    [InlineData("NB")]
    [InlineData("NS")]
    public void New_brunswick_and_nova_scotia_are_rejected(string province) =>
        Assert.True(ClaimValidator.Validate(Valid(province), Docs, null).ContainsKey("Province"));

    [Fact] public void Company_province_also_excludes_NB_and_NS()
    {
        var request = Valid();
        request.CompanyProvince = "NS";
        Assert.True(ClaimValidator.Validate(request, Docs, null).ContainsKey("CompanyProvince"));
    }

    [Fact] public void Province_dropdown_has_no_NB_or_NS()
    {
        Assert.DoesNotContain(Provinces.Selectable, p => p.Code == "NB" || p.Code == "NS");
        Assert.Contains(Provinces.Selectable, p => p.Code == "ON");
    }

    [Fact] public void A_sin_that_fails_luhn_is_rejected()
    {
        var request = Valid();
        request.Sin = "046454287";
        Assert.True(ClaimValidator.Validate(request, Docs, null).ContainsKey("Sin"));
    }

    [Fact] public void Missing_marks_and_completion_upload_is_rejected() =>
        Assert.True(ClaimValidator.Validate(Valid(), Array.Empty<FileMeta>(), null).ContainsKey("Documents"));

    [Fact] public void Disallowed_or_oversized_files_are_rejected()
    {
        Assert.True(ClaimValidator.Validate(Valid(), new[] { new FileMeta("virus.exe", 100) }, null).ContainsKey("Documents"));
        Assert.True(ClaimValidator.Validate(Valid(), new[] { new FileMeta("big.pdf", ClaimValidator.MaxFileBytes + 1) }, null).ContainsKey("Documents"));
    }

    [Fact] public void End_date_cannot_be_before_start_date()
    {
        var request = Valid();
        request.TrainingTo = new DateTime(2025, 12, 1);
        Assert.True(ClaimValidator.Validate(request, Docs, null).ContainsKey("TrainingTo"));
    }
}

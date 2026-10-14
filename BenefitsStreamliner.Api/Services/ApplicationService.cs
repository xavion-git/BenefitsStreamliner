using BenefitsStreamliner.Api.Data;
using BenefitsStreamliner.Core.DTOs;
using BenefitsStreamliner.Core.Models;
using BenefitsStreamliner.Core.Rules;
using Microsoft.AspNetCore.DataProtection;

namespace BenefitsStreamliner.Api.Services;

// Stores a validated claim with Status = New. The duplicate check and the eligibility rules
// run next, in BenefitsCheckService (the flow's "Store application" -> "Duplicate claim found?" order).
public class ApplicationService(
    AppDbContext db,
    IDataProtectionProvider dataProtection,
    IConfiguration config,
    IWebHostEnvironment env,
    ILogger<ApplicationService> logger)
{
    // The SIN is encrypted before it touches the database.
    private readonly IDataProtector _sinProtector = dataProtection.CreateProtector("BenefitsStreamliner.Sin.v1");

    /// Returns the new application ID. The request has already passed ClaimValidator.
    public async Task<string> SubmitAsync(
        ApplicationRequest req,
        IReadOnlyList<IFormFile> completionDocs,
        IReadOnlyList<IFormFile> paymentDocs,
        CancellationToken ct = default)
    {
        var sin = SinValidator.Digits(req.Sin);
        var now = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var entity = new Application
        {
            ApplicationId = "TEMP-" + Guid.NewGuid().ToString("N"),   // unique placeholder until we have the Id
            FirstName = Clean(req.FirstName),
            LastName = Clean(req.LastName),
            SinEncrypted = _sinProtector.Protect(sin),
            SinLast3 = sin[^3..],
            Phone = FormatPhone(req.Phone),
            Email = Clean(req.Email),
            MailingAddress = Clean(req.MailingAddress),
            City = Clean(req.City),
            Province = Clean(req.Province).ToUpperInvariant(),
            PostalCode = NormalizePostal(req.PostalCode),
            CertificateNumber = Clean(req.CertificateNumber),
            CompanyName = Clean(req.CompanyName),
            CompanyAddress = Clean(req.CompanyAddress),
            CompanyCity = Clean(req.CompanyCity),
            CompanyProvince = Clean(req.CompanyProvince).ToUpperInvariant(),
            CompanyPostalCode = NormalizePostal(req.CompanyPostalCode),
            Trade = Clean(req.Trade),
            TrainingYear = req.TrainingYear!.Value,
            TrainingProvince = Clean(req.TrainingProvince).ToUpperInvariant(),
            TrainingFrom = req.TrainingFrom!.Value.Date,
            TrainingTo = req.TrainingTo!.Value.Date,
            ReturnedToWork = req.ReturnedToWork!.Value.Date,
            PayTo = Clean(req.PayTo),
            TermsAcceptedAt = now,
            EmployeeSignature = Clean(req.EmployeeSignature),
            EmployeeSignDate = req.EmployeeSignDate!.Value.Date,
            RepName = Clean(req.RepName),
            RepPosition = Clean(req.RepPosition),
            RepSignature = Clean(req.RepSignature),
            RepSignDate = req.RepSignDate!.Value.Date,
            Status = ApplicationStatus.New,
            CreatedAt = now
        };

        db.Applications.Add(entity);
        await db.SaveChangesAsync(ct);                       // gets the auto-increment Id

        // The ID is generated server-side from the DB identity: APP-000001, APP-000002, ...
        entity.ApplicationId = $"APP-{entity.Id:D6}";
        var folder = Path.Combine(UploadRoot(), entity.ApplicationId);

        try
        {
            Directory.CreateDirectory(folder);
            foreach (var file in completionDocs)
                await AddDocumentAsync(entity, folder, file, DocumentTypes.MarksAndCompletion, ct);
            foreach (var file in paymentDocs)
                await AddDocumentAsync(entity, folder, file, DocumentTypes.ProofOfPayment, ct);

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            TryDeleteFolder(folder);                         // the transaction rolls back when it is disposed
            throw;
        }

        logger.LogInformation("Stored {Id} (New) with {Count} document(s).", entity.ApplicationId, entity.Documents.Count);
        return entity.ApplicationId;
    }

    private async Task AddDocumentAsync(Application app, string folder, IFormFile file, string type, CancellationToken ct)
    {
        var storedName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        await using (var stream = File.Create(Path.Combine(folder, storedName)))
            await file.CopyToAsync(stream, ct);

        var original = Path.GetFileName(file.FileName);
        app.Documents.Add(new ApplicationDocument
        {
            DocumentType = type,
            OriginalFileName = original.Length > 255 ? original[..255] : original,
            StoredFileName = storedName,
            SizeBytes = file.Length
        });
    }

    private string UploadRoot()
    {
        var configured = config["Uploads:Path"] ?? "Uploads";
        return Path.IsPathRooted(configured) ? configured : Path.Combine(env.ContentRootPath, configured);
    }

    private void TryDeleteFolder(string folder)
    {
        try { if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true); }
        catch (Exception ex) { logger.LogWarning(ex, "Could not clean up {Folder}.", folder); }
    }

    private static string Clean(string? value) => (value ?? string.Empty).Trim();

    // "t5l4h5" and "T5L 4H5" both become "T5L 4H5".
    private static string NormalizePostal(string? value)
    {
        var p = new string(Clean(value).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        return p.Length == 6 ? $"{p[..3]} {p[3..]}" : p;
    }

    // 7805551234, 1-780-555-1234 and (780) 555-1234 all become "(780) 555-1234".
    private static string FormatPhone(string? value)
    {
        var d = SinValidator.Digits(value);
        if (d.Length == 11 && d[0] == '1') d = d[1..];
        return d.Length == 10 ? $"({d[..3]}) {d[3..6]}-{d[6..]}" : Clean(value);
    }
}

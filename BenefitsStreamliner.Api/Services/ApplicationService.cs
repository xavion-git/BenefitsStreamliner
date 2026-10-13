using BenefitsStreamliner.Api.Data;
using BenefitsStreamliner.Core.DTOs;
using BenefitsStreamliner.Core.Models;
using BenefitsStreamliner.Core.Rules;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace BenefitsStreamliner.Api.Services;

public class ApplicationService(
    AppDbContext db,
    IDataProtectionProvider dataProtection,
    IConfiguration config,
    IWebHostEnvironment env,
    ILogger<ApplicationService> logger)
{
    // The SIN is encrypted before it touches the database.
    private readonly IDataProtector _sinProtector = dataProtection.CreateProtector("BenefitsStreamliner.Sin.v1");

    /// Success: (applicationId, null).
    /// Duplicate certificate number + tuition code: (null, id of the existing claim).
    /// The request has already passed ClaimValidator.
    public async Task<(string? ApplicationId, string? DuplicateOf)> SubmitAsync(
        ApplicationRequest req, IReadOnlyList<IFormFile> documents, IFormFile? proofOfPayment,
        CancellationToken ct = default)
    {
        var certificate = Clean(req.CertificateNumber).ToUpperInvariant();
        var tuitionCode = Clean(req.TuitionCode).ToUpperInvariant();

        // Duplicate check on certificate number + tuition code
        var duplicate = await db.Applications
            .Where(a => a.CertificateNumber == certificate && a.TuitionCode == tuitionCode)
            .Select(a => a.ApplicationId)
            .FirstOrDefaultAsync(ct);
        if (duplicate is not null) return (null, duplicate);

        var sin = SinValidator.Digits(req.Sin);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var entity = new Application
        {
            ApplicationId = "TEMP-" + Guid.NewGuid().ToString("N"),   // unique placeholder until we have the Id
            EmployeeName = Clean(req.EmployeeName),
            SinEncrypted = _sinProtector.Protect(sin),
            SinLast3 = sin[^3..],
            Phone = Clean(req.Phone),
            Email = Clean(req.Email),
            MailingAddress = Clean(req.MailingAddress),
            City = Clean(req.City),
            Province = Clean(req.Province).ToUpperInvariant(),
            PostalCode = NormalizePostal(req.PostalCode),
            CompanyName = Clean(req.CompanyName),
            CompanyAddress = Clean(req.CompanyAddress),
            CompanyCity = Clean(req.CompanyCity),
            CompanyProvince = Clean(req.CompanyProvince).ToUpperInvariant(),
            CompanyPostalCode = NormalizePostal(req.CompanyPostalCode),
            Trade = Clean(req.Trade),
            TrainingYear = req.TrainingYear!.Value,
            TrainingFrom = req.TrainingFrom!.Value.Date,
            TrainingTo = req.TrainingTo!.Value.Date,
            ReturnedToWork = req.ReturnedToWork!.Value.Date,
            PayTo = Clean(req.PayTo),
            CertificateNumber = certificate,
            TuitionCode = tuitionCode,
            EmployeeSignature = Clean(req.EmployeeSignature),
            EmployeeSignDate = req.EmployeeSignDate!.Value.Date,
            RepName = Clean(req.RepName),
            RepPosition = Clean(req.RepPosition),
            RepSignature = Clean(req.RepSignature),
            RepSignDate = req.RepSignDate!.Value.Date,
            Status = ApplicationStatus.Submitted,
            CreatedAt = DateTime.UtcNow
        };

        db.Applications.Add(entity);
        await db.SaveChangesAsync(ct);                       // gets the auto-increment Id

        // The ID is generated server-side from the DB identity: APP-000001, APP-000002, ...
        entity.ApplicationId = $"APP-{entity.Id:D6}";
        var folder = Path.Combine(UploadRoot(), entity.ApplicationId);

        try
        {
            Directory.CreateDirectory(folder);
            foreach (var file in documents)
                await AddDocumentAsync(entity, folder, file, DocumentTypes.MarksAndCompletion, ct);
            if (proofOfPayment is not null)
                await AddDocumentAsync(entity, folder, proofOfPayment, DocumentTypes.ProofOfPayment, ct);

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            TryDeleteFolder(folder);                         // the transaction rolls back when it is disposed
            throw;
        }

        logger.LogInformation("Saved {Id} with {Count} document(s).", entity.ApplicationId, entity.Documents.Count);
        return (entity.ApplicationId, null);
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

    // "t5l4h5", "T5L-4H5" and "T5L 4H5" all become "T5L 4H5".
    private static string NormalizePostal(string? value)
    {
        var p = new string(Clean(value).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        return p.Length == 6 ? $"{p[..3]} {p[3..]}" : p;
    }
}

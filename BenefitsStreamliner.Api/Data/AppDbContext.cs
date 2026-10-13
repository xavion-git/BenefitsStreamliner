using BenefitsStreamliner.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BenefitsStreamliner.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<ApplicationDocument> ApplicationDocuments => Set<ApplicationDocument>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Application>(e =>
        {
            e.Property(x => x.ApplicationId).HasMaxLength(40);
            e.HasIndex(x => x.ApplicationId).IsUnique();

            // Duplicate check: one claim per certificate number + tuition code
            e.Property(x => x.CertificateNumber).HasMaxLength(30);
            e.Property(x => x.TuitionCode).HasMaxLength(20);
            e.HasIndex(x => new { x.CertificateNumber, x.TuitionCode }).IsUnique();

            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

            e.Property(x => x.EmployeeName).HasMaxLength(100);
            e.Property(x => x.SinEncrypted).HasMaxLength(500);
            e.Property(x => x.SinLast3).HasMaxLength(3);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.Email).HasMaxLength(150);
            e.Property(x => x.MailingAddress).HasMaxLength(200);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.Province).HasMaxLength(2);
            e.Property(x => x.PostalCode).HasMaxLength(7);
            e.Property(x => x.CompanyName).HasMaxLength(150);
            e.Property(x => x.CompanyAddress).HasMaxLength(200);
            e.Property(x => x.CompanyCity).HasMaxLength(100);
            e.Property(x => x.CompanyProvince).HasMaxLength(2);
            e.Property(x => x.CompanyPostalCode).HasMaxLength(7);
            e.Property(x => x.Trade).HasMaxLength(100);
            e.Property(x => x.PayTo).HasMaxLength(10);
            e.Property(x => x.EmployeeSignature).HasMaxLength(100);
            e.Property(x => x.RepName).HasMaxLength(100);
            e.Property(x => x.RepPosition).HasMaxLength(100);
            e.Property(x => x.RepSignature).HasMaxLength(100);
        });

        b.Entity<ApplicationDocument>(e =>
        {
            e.HasOne<Application>().WithMany(a => a.Documents)
                .HasForeignKey(d => d.ApplicationRowId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.DocumentType).HasMaxLength(30);
            e.Property(x => x.OriginalFileName).HasMaxLength(255);
            e.Property(x => x.StoredFileName).HasMaxLength(100);
        });

        b.Entity<Recommendation>(e =>
        {
            e.Property(x => x.ApplicationId).HasMaxLength(40);
            e.HasIndex(x => x.ApplicationId);
            e.Property(x => x.Decision).HasColumnName("Recommendation").HasMaxLength(50);
            e.Property(x => x.Reason).HasMaxLength(500);
        });
    }
}

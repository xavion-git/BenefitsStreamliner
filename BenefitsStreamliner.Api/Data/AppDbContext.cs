using BenefitsStreamliner.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BenefitsStreamliner.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Application>(e =>
        {
            e.Property(x => x.ApplicationId).HasMaxLength(40);
            e.HasIndex(x => x.ApplicationId).IsUnique();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Income).HasPrecision(12, 2);
            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
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

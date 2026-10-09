using BenefitsStreamliner.Api.Data;
using BenefitsStreamliner.Core.DTOs;
using BenefitsStreamliner.Core.Models;

namespace BenefitsStreamliner.Api.Services;

public class ApplicationService(AppDbContext db)
{
    // The ID is generated server-side from the DB identity: APP-000001, APP-000002, ...
    public async Task<string> SubmitAsync(ApplicationRequest req)
    {
        var entity = new Application
        {
            ApplicationId = "TEMP-" + Guid.NewGuid().ToString("N"),   // unique placeholder
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),
            DateOfBirth = req.DateOfBirth!.Value.Date,
            Income = req.Income,
            Status = ApplicationStatus.Submitted,
            CreatedAt = DateTime.UtcNow
        };

        db.Applications.Add(entity);
        await db.SaveChangesAsync();                       // gets the auto-increment Id

        entity.ApplicationId = $"APP-{entity.Id:D6}";
        await db.SaveChangesAsync();
        return entity.ApplicationId;
    }
}
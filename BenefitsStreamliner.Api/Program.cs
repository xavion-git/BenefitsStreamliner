using BenefitsStreamliner.Api.Data;
using BenefitsStreamliner.Api.Services;
using BenefitsStreamliner.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var provider = builder.Configuration["Database:Provider"] ?? "MySql";
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:Default (use user-secrets or an env var).");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    switch (provider.ToLowerInvariant())
    {
        case "sqlserver":
            options.UseSqlServer(connectionString);
            break;
        case "sqlite":
            options.UseSqlite(connectionString);
            break;
        default:
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            break;
    }
});

builder.Services.AddScoped<ApplicationService>();
builder.Services.AddScoped<BenefitsCheckService>();

// Swap MockBenefitsService for a RealIBenefitsService here later. Nothing else changes.
builder.Services.AddSingleton<IBenefitsService, MockBenefitsService>();
builder.Services.AddSingleton<IFinanceService, MockFinanceService>();

builder.Services.AddSingleton<RetryQueue>();
builder.Services.AddHostedService<RetryWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();   // dev convenience
}

app.MapControllers();
app.Run();
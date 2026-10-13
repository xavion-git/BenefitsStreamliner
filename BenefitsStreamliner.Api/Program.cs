using BenefitsStreamliner.Api.Data;
using BenefitsStreamliner.Api.Services;
using BenefitsStreamliner.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDataProtection();   // encrypts the SIN before it is stored

// SqlServer is the default. Set Database:Provider to "MySql" or "Sqlite" to switch.
var provider = builder.Configuration["Database:Provider"] ?? "SqlServer";
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:Default (use user-secrets or an env var).");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    switch (provider.ToLowerInvariant())
    {
        case "mysql":
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            break;
        case "sqlite":
            options.UseSqlite(connectionString);
            break;
        default:
            options.UseSqlServer(connectionString);
            break;
    }
});

builder.Services.AddScoped<ApplicationService>();
builder.Services.AddScoped<BenefitsCheckService>();
builder.Services.AddScoped<DecisionService>();

// Swap the mocks for real implementations here later. Nothing else changes.
builder.Services.AddSingleton<IBenefitsService, MockBenefitsService>();
builder.Services.AddSingleton<IFinanceService, MockFinanceService>();
builder.Services.AddSingleton<IBusinessCentralService, MockBusinessCentralService>();

builder.Services.AddSingleton<RetryQueue>();
builder.Services.AddHostedService<RetryWorker>();           // iBenefits unavailable -> retry
builder.Services.AddHostedService<HoursRecheckWorker>();    // Waiting for Hours -> monthly re-check

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();   // dev convenience
}

app.MapControllers();
app.Run();

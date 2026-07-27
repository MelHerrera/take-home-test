using Fundo.Application;
using Fundo.Applications.WebApi.Middleware;
using Fundo.Infrastructure;
using Fundo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

const string AngularDevCorsPolicy = "AngularDev";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console()
    .WriteTo.File(new CompactJsonFormatter(),"logs/log-.json",rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30) // keep the last 30 daily files, delete older ones automatically
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddControllers();

    // Angular's dev server (ng serve, localhost:4200) and this API run on different ports, which
    // browsers treat as different origins - CORS must explicitly allow it or the browser blocks
    // every request. Scoped to the known dev server origin only, not a wildcard.
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(AngularDevCorsPolicy, policy =>
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    });

    var app = builder.Build();

    // Applies any pending migrations on startup. Migrate() is idempotent (checks
    // __EFMigrationsHistory), so this is safe to run every time the app starts,
    // whether locally against LocalDB or inside the Docker container. Skipped when
    // SkipAutoMigrate=true: integration tests build their own SQLite schema via
    // EnsureCreated (see CustomWebApplicationFactory), which cannot be mixed with Migrate().
    if (!app.Configuration.GetValue<bool>("SkipAutoMigrate"))
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FundoDbContext>();
        dbContext.Database.Migrate();
    }

    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseRouting();
    app.UseCors(AngularDevCorsPolicy);
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }

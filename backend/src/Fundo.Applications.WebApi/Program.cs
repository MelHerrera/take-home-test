using Fundo.Application;
using Fundo.Applications.WebApi.Middleware;
using Fundo.Infrastructure;
using Fundo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();

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

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
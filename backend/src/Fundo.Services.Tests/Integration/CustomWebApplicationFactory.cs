using Fundo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fundo.Services.Tests.Integration;

// Deliberately NOT shared via IClassFixture: each test gets its own factory/connection/instance
// (xUnit's default per-test class instantiation) so seeded data never leaks between tests
// regardless of execution order.
public class CustomWebApplicationFactory(bool bypassAuthentication = true) : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Stay on "Development" so appsettings.Development.json's (real, working) connection
        // string satisfies AddInfrastructure's eager GetConnectionString(...) check in Program.cs.
        // That value is discarded seconds later when the SQL Server DbContext registration below
        // gets replaced with SQLite - it's never actually connected to.
        builder.UseEnvironment("Development");

        // Program.cs runs dbContext.Database.Migrate() on every startup, which is incompatible
        // with EnsureCreated() below (they're mutually exclusive schema-initialization strategies).
        // This flag tells Program.cs to skip that call under tests.
        builder.ConfigureAppConfiguration((_, configBuilder) =>
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SkipAutoMigrate"] = "true",
            }));

        builder.ConfigureServices(services =>
        {
            // Removing only DbContextOptions<FundoDbContext> is not enough on EF Core 7+: AddDbContext
            // also registers IDbContextOptionsConfiguration<FundoDbContext> entries that ACCUMULATE
            // across multiple AddDbContext calls instead of being replaced. Leaving the original
            // UseSqlServer configuration registered causes EF Core to see two providers at once.
            services.RemoveAll<DbContextOptions<FundoDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<FundoDbContext>>();

            _connection.Open();

            services.AddDbContext<FundoDbContext>(options => options.UseSqlite(_connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<FundoDbContext>();
            // EnsureCreated (not migrations - those are SQL Server-specific) builds the schema
            // straight from the current model, including HasData seed rows.
            dbContext.Database.EnsureCreated();

            // Most tests care about business/HTTP behavior, not JWT mechanics, so they opt into
            // this substitution (the default). Auth-specific tests construct the factory with
            // bypassAuthentication: false to exercise the real JwtBearer handler and get a real 401.
            if (bypassAuthentication)
            {
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}

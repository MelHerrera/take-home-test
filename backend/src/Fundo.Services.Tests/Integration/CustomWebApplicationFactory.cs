using Fundo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fundo.Services.Tests.Integration;

// Deliberately NOT shared via IClassFixture: each test gets its own factory/connection/instance
// (xUnit's default per-test class instantiation) so seeded data never leaks between tests
// regardless of execution order.
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}

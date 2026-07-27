using Fundo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fundo.Infrastructure.Persistence;

public class FundoDbContext(DbContextOptions<FundoDbContext> options) : DbContext(options)
{
    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FundoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

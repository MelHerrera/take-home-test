using Fundo.Application.Interfaces;
using Fundo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fundo.Infrastructure.Persistence;

public class LoanRepository(FundoDbContext dbContext) : ILoanRepository
{
    // Tracked (no AsNoTracking): RegisterPaymentAsync mutates the returned entity and relies on
    // the change tracker to persist it via SaveChangesAsync without an explicit Update call.
    public Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Loans.FirstOrDefaultAsync(loan => loan.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Loan>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Loans.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(Loan loan, CancellationToken cancellationToken = default) =>
        await dbContext.Loans.AddAsync(loan, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

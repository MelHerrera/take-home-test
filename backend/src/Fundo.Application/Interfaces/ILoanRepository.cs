using Fundo.Domain.Entities;

namespace Fundo.Application.Interfaces;

public interface ILoanRepository
{
    Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Loan>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Loan loan, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

using Fundo.Application.Dtos;

namespace Fundo.Application.Interfaces;

public interface ILoanService
{
    Task<LoanResponse> CreateLoanAsync(CreateLoanRequest request, CancellationToken cancellationToken = default);

    Task<LoanResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LoanResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<LoanResponse> RegisterPaymentAsync(Guid id, RegisterPaymentRequest request, CancellationToken cancellationToken = default);
}

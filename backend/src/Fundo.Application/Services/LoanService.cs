using Fundo.Application;
using Fundo.Application.Dtos;
using Fundo.Application.Exceptions;
using Fundo.Application.Interfaces;
using Fundo.Application.Mappings;
using Fundo.Domain.Entities;
using Fundo.Domain.Enums;
using Microsoft.Extensions.Localization;

namespace Fundo.Application.Services;

public class LoanService(ILoanRepository loanRepository, IStringLocalizer<ValidationMessages> localizer) : ILoanService
{
    public async Task<LoanResponse> CreateLoanAsync(CreateLoanRequest request, CancellationToken cancellationToken = default)
    {
        EnsureValidCreateRequest(request);

        var loan = new Loan(request.ApplicantName, request.AmountRequested);

        await loanRepository.AddAsync(loan, cancellationToken);
        await loanRepository.SaveChangesAsync(cancellationToken);

        return loan.ToResponse();
    }

    public async Task<LoanResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var loan = await GetExistingLoanAsync(id, cancellationToken);
        return loan.ToResponse();
    }

    public async Task<IReadOnlyList<LoanResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var loans = await loanRepository.GetAllAsync(cancellationToken);
        return loans.Select(loan => loan.ToResponse()).ToList();
    }

    public async Task<LoanResponse> RegisterPaymentAsync(Guid id, RegisterPaymentRequest request, CancellationToken cancellationToken = default)
    {
        EnsureValidPaymentAmount(request);

        var loan = await GetExistingLoanAsync(id, cancellationToken);

        EnsureCanRegisterPayment(loan, request);

        loan.RegisterPayment(request.Amount);
        await loanRepository.SaveChangesAsync(cancellationToken);

        return loan.ToResponse();
    }

    private void EnsureValidCreateRequest(CreateLoanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ApplicantName))
            throw new ValidationException(localizer["ApplicantNameRequired"]);
        if (request.AmountRequested <= 0)
            throw new ValidationException(localizer["AmountRequestedInvalid"]);
    }

    private void EnsureValidPaymentAmount(RegisterPaymentRequest request)
    {
        if (request.Amount <= 0)
            throw new ValidationException(localizer["PaymentAmountInvalid"]);
    }

    private void EnsureCanRegisterPayment(Loan loan, RegisterPaymentRequest request)
    {
        if (loan.Status == LoanStatus.Paid)
            throw new ValidationException(localizer["LoanAlreadyPaid"]);
        if (request.Amount > loan.CurrentBalance)
            throw new ValidationException(localizer["PaymentExceedsBalance"]);
    }

    private async Task<Loan> GetExistingLoanAsync(Guid id, CancellationToken cancellationToken)
    {
        return await loanRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(localizer["LoanNotFound", id]);
    }
}

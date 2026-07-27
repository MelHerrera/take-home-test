namespace Fundo.Application.Dtos;

public record LoanResponse(
    Guid Id,
    string ApplicantName,
    decimal AmountRequested,
    decimal CurrentBalance,
    string Status);

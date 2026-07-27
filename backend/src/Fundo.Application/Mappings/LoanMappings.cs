using Fundo.Application.Dtos;
using Fundo.Domain.Entities;

namespace Fundo.Application.Mappings;

public static class LoanMappings
{
    public static LoanResponse ToResponse(this Loan loan) =>
        new(loan.Id, loan.ApplicantName, loan.AmountRequested, loan.CurrentBalance, loan.Status.ToString());
}

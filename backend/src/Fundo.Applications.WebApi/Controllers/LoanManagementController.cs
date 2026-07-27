using Fundo.Application.Dtos;
using Fundo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fundo.Applications.WebApi.Controllers;

[ApiController]
[Route("loans")]
[Authorize]
public class LoanManagementController(ILoanService loanService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<LoanResponse>> Create(CreateLoanRequest request, CancellationToken cancellationToken)
    {
        var loan = await loanService.CreateLoanAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = loan.Id }, loan);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LoanResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var loan = await loanService.GetByIdAsync(id, cancellationToken);
        return Ok(loan);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LoanResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var loans = await loanService.GetAllAsync(cancellationToken);
        return Ok(loans);
    }

    [HttpPost("{id:guid}/payment")]
    public async Task<ActionResult<LoanResponse>> RegisterPayment(Guid id, RegisterPaymentRequest request, CancellationToken cancellationToken)
    {
        var loan = await loanService.RegisterPaymentAsync(id, request, cancellationToken);
        return Ok(loan);
    }
}

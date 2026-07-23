using Fundo.Application;
using Fundo.Application.Dtos;
using Fundo.Application.Exceptions;
using Fundo.Application.Interfaces;
using Fundo.Application.Services;
using Fundo.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Fundo.Services.Tests.Unit.Services;

public class LoanServiceTests
{
    private readonly Mock<ILoanRepository> _repository = new();
    private readonly LoanService _sut;

    public LoanServiceTests()
    {
        var localizerFactory = new ResourceManagerStringLocalizerFactory(
            Options.Create(new LocalizationOptions()),
            NullLoggerFactory.Instance);
        var localizer = new StringLocalizer<ValidationMessages>(localizerFactory);

        _sut = new LoanService(_repository.Object, localizer);
    }

    [Fact]
    public async Task CreateLoanAsync_WithValidRequest_PersistsAndReturnsActiveLoan()
    {
        var request = new CreateLoanRequest("John Doe", 25000m);

        var result = await _sut.CreateLoanAsync(request);

        result.ApplicantName.Should().Be("John Doe");
        result.AmountRequested.Should().Be(25000m);
        result.CurrentBalance.Should().Be(25000m);
        result.Status.Should().Be("Active");
        _repository.Verify(r => r.AddAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateLoanAsync_WithEmptyApplicantName_ThrowsValidationExceptionWithResourceMessage()
    {
        var request = new CreateLoanRequest("", 25000m);

        var act = () => _sut.CreateLoanAsync(request);

        (await act.Should().ThrowAsync<ValidationException>())
            .WithMessage("Applicant name is required.");
    }

    [Fact]
    public async Task CreateLoanAsync_WithNonPositiveAmount_ThrowsValidationExceptionWithResourceMessage()
    {
        var request = new CreateLoanRequest("John Doe", 0m);

        var act = () => _sut.CreateLoanAsync(request);

        (await act.Should().ThrowAsync<ValidationException>())
            .WithMessage("Amount requested must be greater than zero.");
    }

    [Fact]
    public async Task GetByIdAsync_WhenLoanExists_ReturnsMappedResponse()
    {
        var loan = new Loan("Jane Smith", 15000m);
        _repository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var result = await _sut.GetByIdAsync(loan.Id);

        result.Id.Should().Be(loan.Id);
        result.ApplicantName.Should().Be("Jane Smith");
    }

    [Fact]
    public async Task GetByIdAsync_WhenLoanDoesNotExist_ThrowsNotFoundExceptionWithFormattedResourceMessage()
    {
        var missingId = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((Loan?)null);

        var act = () => _sut.GetByIdAsync(missingId);

        (await act.Should().ThrowAsync<NotFoundException>())
            .WithMessage($"Loan with id '{missingId}' was not found.");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllLoansMapped()
    {
        var loans = new List<Loan> { new("John Doe", 25000m), new("Jane Smith", 15000m) };
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(loans);

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task RegisterPaymentAsync_WithValidAmount_ReducesBalance()
    {
        var loan = new Loan("John Doe", 25000m);
        _repository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var result = await _sut.RegisterPaymentAsync(loan.Id, new RegisterPaymentRequest(6250m));

        result.CurrentBalance.Should().Be(18750m);
        result.Status.Should().Be("Active");
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterPaymentAsync_WhenBalanceReachesZero_MarksLoanAsPaid()
    {
        var loan = new Loan("John Doe", 25000m);
        _repository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var result = await _sut.RegisterPaymentAsync(loan.Id, new RegisterPaymentRequest(25000m));

        result.Status.Should().Be("Paid");
    }

    [Fact]
    public async Task RegisterPaymentAsync_WithNonPositiveAmount_ThrowsValidationExceptionWithResourceMessage()
    {
        var act = () => _sut.RegisterPaymentAsync(Guid.NewGuid(), new RegisterPaymentRequest(0m));

        (await act.Should().ThrowAsync<ValidationException>())
            .WithMessage("Payment amount must be greater than zero.");
    }

    [Fact]
    public async Task RegisterPaymentAsync_WhenAmountExceedsBalance_ThrowsValidationExceptionWithResourceMessage()
    {
        var loan = new Loan("John Doe", 25000m);
        _repository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var act = () => _sut.RegisterPaymentAsync(loan.Id, new RegisterPaymentRequest(30000m));

        (await act.Should().ThrowAsync<ValidationException>())
            .WithMessage("Payment amount cannot exceed the current balance.");
    }

    [Fact]
    public async Task RegisterPaymentAsync_WhenLoanAlreadyPaid_ThrowsValidationExceptionWithResourceMessage()
    {
        var loan = new Loan("John Doe", 25000m);
        loan.RegisterPayment(25000m);
        _repository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var act = () => _sut.RegisterPaymentAsync(loan.Id, new RegisterPaymentRequest(100m));

        (await act.Should().ThrowAsync<ValidationException>())
            .WithMessage("Cannot register a payment on a loan that is already paid.");
    }
}

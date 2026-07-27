using System.Net;
using System.Net.Http.Json;
using Fundo.Application.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Fundo.Services.Tests.Integration;

public class LoanManagementControllerTests : IDisposable
{
    private static readonly Guid JohnDoeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid JaneSmithId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public LoanManagementControllerTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsSeededLoans()
    {
        var response = await _client.GetAsync("/loans");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var loans = await response.Content.ReadFromJsonAsync<List<LoanResponse>>();
        loans.Should().HaveCount(3);
        loans.Should().Contain(loan => loan.ApplicantName == "John Doe");
    }

    [Fact]
    public async Task GetById_WhenLoanExists_ReturnsLoan()
    {
        var response = await _client.GetAsync($"/loans/{JohnDoeId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var loan = await response.Content.ReadFromJsonAsync<LoanResponse>();
        loan!.ApplicantName.Should().Be("John Doe");
        loan.CurrentBalance.Should().Be(18750m);
    }

    [Fact]
    public async Task GetById_WhenLoanDoesNotExist_ReturnsNotFoundProblemDetails()
    {
        var missingId = Guid.NewGuid();

        var response = await _client.GetAsync($"/loans/{missingId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Detail.Should().Be($"Loan with id '{missingId}' was not found.");
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedWithLocationHeader()
    {
        var request = new CreateLoanRequest("Maria Garcia", 10000m);

        var response = await _client.PostAsJsonAsync("/loans", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var loan = await response.Content.ReadFromJsonAsync<LoanResponse>();
        response.Headers.Location.Should().Be($"http://localhost/loans/{loan!.Id}");
        loan.CurrentBalance.Should().Be(10000m);
        loan.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Create_WithEmptyApplicantName_ReturnsBadRequestProblemDetails()
    {
        var request = new CreateLoanRequest("", 10000m);

        var response = await _client.PostAsJsonAsync("/loans", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Detail.Should().Be("Applicant name is required.");
    }

    [Fact]
    public async Task RegisterPayment_WithValidAmount_ReducesBalance()
    {
        var response = await _client.PostAsJsonAsync($"/loans/{JohnDoeId}/payment", new RegisterPaymentRequest(6250m));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var loan = await response.Content.ReadFromJsonAsync<LoanResponse>();
        loan!.CurrentBalance.Should().Be(12500m);
    }

    [Fact]
    public async Task RegisterPayment_WhenAmountExceedsBalance_ReturnsBadRequestProblemDetails()
    {
        var response = await _client.PostAsJsonAsync($"/loans/{JohnDoeId}/payment", new RegisterPaymentRequest(99999m));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Detail.Should().Be("Payment amount cannot exceed the current balance.");
    }

    [Fact]
    public async Task RegisterPayment_WhenLoanAlreadyPaid_ReturnsBadRequestProblemDetails()
    {
        var response = await _client.PostAsJsonAsync($"/loans/{JaneSmithId}/payment", new RegisterPaymentRequest(100m));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Detail.Should().Be("Cannot register a payment on a loan that is already paid.");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Fundo.Applications.WebApi.Auth;
using FluentAssertions;
using Xunit;

namespace Fundo.Services.Tests.Integration;

// Uses bypassAuthentication: false, unlike LoanManagementControllerTests, so these tests
// exercise the real JwtBearer handler instead of TestAuthHandler's always-succeed shortcut.
public class AuthenticationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new(bypassAuthentication: false);
    private readonly HttpClient _client;

    public AuthenticationTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/loans");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task IssueToken_WithValidCredentials_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/auth/token", new TokenRequest("admin", "ChangeMe123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        token!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task IssueToken_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/auth/token", new TokenRequest("admin", "wrong-password"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithValidToken_ReturnsOk()
    {
        var tokenResponse = await _client.PostAsJsonAsync("/auth/token", new TokenRequest("admin", "ChangeMe123!"));
        var token = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);

        var response = await _client.GetAsync("/loans");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}

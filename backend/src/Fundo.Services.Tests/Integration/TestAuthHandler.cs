using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fundo.Services.Tests.Integration;

// Replaces real JWT validation in tests that opt in (see CustomWebApplicationFactory's
// bypassAuthentication flag): authenticates every request as a fixed test user, so most
// integration tests can exercise [Authorize]-protected endpoints without generating and
// attaching real JWTs. The real 401 behavior is verified separately, against a factory
// instance that does NOT install this handler (see LoanManagementControllerAuthTests).
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "test-user") };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

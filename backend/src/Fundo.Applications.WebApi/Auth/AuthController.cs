using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Fundo.Applications.WebApi.Auth;

// Demo-only credential check: this assessment has no user management system (no Users
// table, no registration flow), so a single hardcoded account stands in for a real
// identity provider, purely to demonstrate the JWT issuance/validation mechanics end to
// end. A real system would validate against a persisted, hashed credential store or an
// external identity provider (OAuth2/OpenID Connect), never an in-code check like this.
[ApiController]
[Route("auth")]
public class AuthController(IOptions<JwtSettings> jwtSettings) : ControllerBase
{
    private const string DemoUsername = "admin";
    private const string DemoPassword = "ChangeMe123!";

    [HttpPost("token")]
    public ActionResult<TokenResponse> IssueToken(TokenRequest request)
    {
        if (request.Username != DemoUsername || request.Password != DemoPassword)
            return Unauthorized();

        var settings = jwtSettings.Value;
        var expiresAt = DateTime.UtcNow.AddMinutes(settings.ExpiryMinutes);

        var claims = new[] { new Claim(ClaimTypes.Name, request.Username) };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(new TokenResponse(accessToken, expiresAt));
    }
}

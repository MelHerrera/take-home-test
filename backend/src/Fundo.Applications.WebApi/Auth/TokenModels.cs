namespace Fundo.Applications.WebApi.Auth;

public record TokenRequest(string Username, string Password);

public record TokenResponse(string AccessToken, DateTime ExpiresAtUtc);

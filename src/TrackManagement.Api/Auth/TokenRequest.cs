namespace TrackManagement.Api.Auth;

public record TokenRequest(string? Username, string? Password);

public record TokenResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc);

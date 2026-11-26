namespace TrackManagement.Api.Auth;

public interface ITokenService
{
    TokenResult CreateToken(string username);
}

public record TokenResult(string AccessToken, DateTime ExpiresAtUtc);

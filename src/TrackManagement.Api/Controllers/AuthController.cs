using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TrackManagement.Api.Auth;

namespace TrackManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokens;
    private readonly DemoCredentialsOptions _demo;
    private readonly IWebHostEnvironment _env;

    public AuthController(ITokenService tokens, IOptions<DemoCredentialsOptions> demo, IWebHostEnvironment env)
    {
        _tokens = tokens;
        _demo = demo.Value;
        _env = env;
    }

    /// <summary>Dev-only demo token. Returns 404 outside the Development environment.</summary>
    [HttpPost("token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TokenResponse> CreateToken(TokenRequest request)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        var valid = !string.IsNullOrEmpty(_demo.Username)
                    && !string.IsNullOrEmpty(_demo.Password)
                    && request.Username is not null
                    && request.Password is not null;

        // Always evaluate both comparisons so timing does not reveal which one failed.
        var userOk = FixedTimeEquals(request.Username, _demo.Username);
        var passOk = FixedTimeEquals(request.Password, _demo.Password);

        if (!(valid && userOk && passOk))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Invalid credentials.");
        }

        var token = _tokens.CreateToken(_demo.Username);
        return Ok(new TokenResponse(token.AccessToken, "Bearer", token.ExpiresAtUtc));
    }

    // Hash both values first so the comparison is constant-time and independent of input length.
    private static bool FixedTimeEquals(string? a, string? b)
    {
        var hashA = SHA256.HashData(Encoding.UTF8.GetBytes(a ?? string.Empty));
        var hashB = SHA256.HashData(Encoding.UTF8.GetBytes(b ?? string.Empty));
        return CryptographicOperations.FixedTimeEquals(hashA, hashB);
    }
}

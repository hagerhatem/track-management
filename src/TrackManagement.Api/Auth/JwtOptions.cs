namespace TrackManagement.Api.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinKeyLength = 32;
    public const int LifetimeMinutes = 60;

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}

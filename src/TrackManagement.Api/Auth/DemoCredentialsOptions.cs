namespace TrackManagement.Api.Auth;

public class DemoCredentialsOptions
{
    public const string SectionName = "Demo";

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

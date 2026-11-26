namespace TrackManagement.Api.Cors;

public static class FrontendCorsExtensions
{
    public const string PolicyName = "FrontendPolicy";

    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?.Where(o => !string.IsNullOrWhiteSpace(o) && o != "*")
            .ToArray();

        if (origins is null || origins.Length == 0)
        {
            throw new InvalidOperationException(
                "Cors:AllowedOrigins must contain at least one explicit origin (wildcards are not allowed).");
        }

        services.AddCors(options => options.AddPolicy(PolicyName, policy =>
            policy.WithOrigins(origins)
                .WithMethods("GET")
                .WithHeaders("Content-Type", "Authorization")));

        return services;
    }
}

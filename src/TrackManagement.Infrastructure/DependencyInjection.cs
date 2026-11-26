using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrackManagement.Application.Artists;
using TrackManagement.Application.Tracks;
using TrackManagement.Infrastructure.Persistence;
using TrackManagement.Infrastructure.Persistence.Seeding;
using TrackManagement.Infrastructure.Services;

namespace TrackManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string DefaultConnection is not configured. " +
                "Set it in appsettings.json, user-secrets or the ConnectionStrings__DefaultConnection env var.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IArtistService, ArtistService>();
        services.AddScoped<ITrackService, TrackService>();
        services.AddScoped<ITrackWorkflowService, TrackWorkflowService>();
        services.AddScoped<DbSeeder>();

        return services;
    }
}

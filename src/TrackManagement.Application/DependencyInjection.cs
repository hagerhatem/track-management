using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TrackManagement.Application.Artists;
using TrackManagement.Application.Tracks;

namespace TrackManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateArtistRequest>, CreateArtistRequestValidator>();
        services.AddScoped<IValidator<CreateTrackRequest>, CreateTrackRequestValidator>();

        return services;
    }
}
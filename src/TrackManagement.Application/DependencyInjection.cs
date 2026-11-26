using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TrackManagement.Application.Artists;

namespace TrackManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateArtistRequest>, CreateArtistRequestValidator>();

        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Tours.Domain.Interfaces;
using Tours.Infrastructure.Weather.Services;

namespace Tours.Infrastructure.Weather.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWeatherInfrastructure(this IServiceCollection services)
    {
        services.AddHttpClient<IWeatherService, OpenMeteoWeatherService>();
        return services;
    }
}
using Microsoft.Extensions.DependencyInjection;
using Tours.Application.Interfaces;
using Tours.Application.Services;

namespace Tours.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IItineraryService, ItineraryService>();
        return services;
    }
}
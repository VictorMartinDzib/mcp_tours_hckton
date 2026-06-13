using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tours.Domain.Interfaces;
using Tours.Infrastructure.Postgres.Persistence;
using Tours.Infrastructure.Postgres.Repositories;

namespace Tours.Infrastructure.Postgres.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5432;Database=tours;Username=postgres;Password=postgres";

        services.AddDbContext<ToursDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<IItineraryRepository, ItineraryRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
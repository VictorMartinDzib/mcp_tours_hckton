using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tours.Domain.Interfaces;
using Tours.Infrastructure.Postgres.Repositories;
using Tours.Infrastructure.Postgres.Persistence;

namespace Tours.Infrastructure.Postgres.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5432;Database=tours;Username=postgres;Password=postgres";

        services.AddSingleton(new PostgresConnectionFactory(connectionString));
        services.AddSingleton<PostgresMigrationRunner>();
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<IItineraryRepository, ItineraryRepository>();
        services.AddScoped<IUnitOfWork, DapperUnitOfWork>();

        return services;
    }
}

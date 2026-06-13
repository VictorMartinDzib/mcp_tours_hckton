using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tours.Domain.Interfaces;
using Tours.Infrastructure.Postgres.Persistence;
using Tours.Infrastructure.Postgres.Seed;

namespace Tours.Infrastructure.Postgres.Extensions;

public static class DatabaseInitializationExtensions
{
    public static async Task ApplyMigrationsAndSeedAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ToursDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        var activityRepository = scope.ServiceProvider.GetRequiredService<IActivityRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        if (await activityRepository.CountAsync(cancellationToken) < 100)
        {
            await activityRepository.SeedAsync(ActivitySeedFactory.Build(), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
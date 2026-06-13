using Microsoft.EntityFrameworkCore;
using Tours.Domain.Entities;
using Tours.Domain.Enums;
using Tours.Domain.Interfaces;
using Tours.Infrastructure.Postgres.Persistence;

namespace Tours.Infrastructure.Postgres.Repositories;

public sealed class ActivityRepository(ToursDbContext dbContext) : IActivityRepository
{
    public Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.Activities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken)
        => dbContext.Activities.CountAsync(cancellationToken);

    public async Task SeedAsync(IEnumerable<Activity> activities, CancellationToken cancellationToken)
    {
        await dbContext.Activities.AddRangeAsync(activities, cancellationToken);
    }

    public Task<List<Activity>> SearchAsync(
        string destination,
        IReadOnlyCollection<ActivityCategory> categories,
        decimal? maxPrice,
        int? minAge,
        bool onlyAvailable,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Activities.AsQueryable();

        if (!string.IsNullOrWhiteSpace(destination))
        {
            query = query.Where(x => x.Destination.ToLower() == destination.Trim().ToLower());
        }

        if (categories.Count != 0)
        {
            query = query.Where(x => categories.Contains(x.Category));
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(x => x.Price <= maxPrice.Value);
        }

        if (minAge.HasValue)
        {
            query = query.Where(x => x.Requirement.MinimumAge <= minAge.Value);
        }

        if (onlyAvailable)
        {
            query = query.Where(x => x.IsAvailable);
        }

        return query
            .OrderBy(x => x.Price)
            .ToListAsync(cancellationToken);
    }
}
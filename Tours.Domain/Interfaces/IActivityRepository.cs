using Tours.Domain.Entities;
using Tours.Domain.Enums;

namespace Tours.Domain.Interfaces;

public interface IActivityRepository
{
    Task<List<Activity>> SearchAsync(
        string destination,
        IReadOnlyCollection<ActivityCategory> categories,
        decimal? maxPrice,
        int? minAge,
        bool onlyAvailable,
        CancellationToken cancellationToken);

    Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<int> CountAsync(CancellationToken cancellationToken);
    Task SeedAsync(IEnumerable<Activity> activities, CancellationToken cancellationToken);
}
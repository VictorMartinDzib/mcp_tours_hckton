using Tours.Application.Dtos;
using Tours.Domain.Enums;

namespace Tours.Application.Interfaces;

public interface IActivityService
{
    Task<IReadOnlyCollection<ActivityResponse>> SearchAsync(
        string destination,
        IReadOnlyCollection<ActivityCategory> categories,
        decimal? maxPrice,
        int? minAge,
        bool onlyAvailable,
        CancellationToken cancellationToken);

    Task<ActivityResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
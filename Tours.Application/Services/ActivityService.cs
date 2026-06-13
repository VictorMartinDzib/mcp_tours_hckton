using Tours.Application.Dtos;
using Tours.Application.Interfaces;
using Tours.Domain.Entities;
using Tours.Domain.Enums;
using Tours.Domain.Interfaces;

namespace Tours.Application.Services;

public sealed class ActivityService(IActivityRepository repository) : IActivityService
{
    public async Task<IReadOnlyCollection<ActivityResponse>> SearchAsync(
        string destination,
        IReadOnlyCollection<ActivityCategory> categories,
        decimal? maxPrice,
        int? minAge,
        bool onlyAvailable,
        CancellationToken cancellationToken)
    {
        var data = await repository.SearchAsync(destination, categories, maxPrice, minAge, onlyAvailable, cancellationToken);
        return data.Select(ToResponse).ToArray();
    }

    public async Task<ActivityResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var activity = await repository.GetByIdAsync(id, cancellationToken);
        return activity is null ? null : ToResponse(activity);
    }

    private static ActivityResponse ToResponse(Activity activity) => new(
        activity.Id,
        activity.Name,
        activity.Description,
        activity.Price,
        activity.DurationMinutes,
        activity.Location,
        activity.Destination,
        activity.IsAvailable,
        activity.Reviews,
        activity.Photos,
        activity.Category,
        activity.Requirement,
        activity.IncludesTransport,
        activity.IncludesGuide,
        activity.ProviderName,
        activity.IsIndoorAlternative);
}
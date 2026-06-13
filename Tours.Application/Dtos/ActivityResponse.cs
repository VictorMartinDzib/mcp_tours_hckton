using Tours.Domain.Enums;
using Tours.Domain.ValueObjects;

namespace Tours.Application.Dtos;

public sealed record ActivityResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int DurationMinutes,
    string Location,
    string Destination,
    bool IsAvailable,
    IReadOnlyCollection<ActivityReview> Reviews,
    IReadOnlyCollection<string> Photos,
    ActivityCategory Category,
    ActivityRequirement Requirement,
    bool IncludesTransport,
    bool IncludesGuide,
    string ProviderName,
    bool IsIndoorAlternative);
using Tours.Domain.Enums;

namespace Tours.Application.Dtos;

public sealed record ItineraryResponse(
    Guid Id,
    string Destination,
    DateOnly StartDate,
    DateOnly EndDate,
    int NumberOfPeople,
    IReadOnlyCollection<int> Ages,
    IReadOnlyCollection<ActivityCategory> Preferences,
    decimal Budget,
    decimal TotalPrice,
    IReadOnlyCollection<ItineraryItemResponse> Items);
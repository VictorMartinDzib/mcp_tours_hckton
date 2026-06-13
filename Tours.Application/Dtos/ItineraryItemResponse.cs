namespace Tours.Application.Dtos;

public sealed record ItineraryItemResponse(
    Guid Id,
    Guid ActivityId,
    DateOnly ScheduledDate,
    bool WeatherRiskDetected,
    string WeatherSummary,
    Guid? SuggestedAlternativeActivityId,
    string Notes);
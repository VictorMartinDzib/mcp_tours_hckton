using Tours.Domain.ValueObjects;

namespace Tours.Domain.Entities;

public sealed class ItineraryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActivityId { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public bool WeatherRiskDetected { get; set; }
    public string WeatherSummary { get; set; } = string.Empty;
    public Guid? SuggestedAlternativeActivityId { get; set; }
    public string Notes { get; set; } = string.Empty;

    public void ApplyWeather(WeatherAssessment assessment)
    {
        WeatherRiskDetected = !assessment.IsSuitable;
        WeatherSummary = assessment.Summary;
    }
}
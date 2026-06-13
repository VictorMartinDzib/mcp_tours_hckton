namespace Tours.Domain.ValueObjects;

public sealed class WeatherAssessment
{
    public DateOnly Date { get; init; }
    public bool IsSuitable { get; init; }
    public string Summary { get; init; } = string.Empty;
    public double RainProbability { get; init; }
}
using Tours.Domain.ValueObjects;

namespace Tours.Domain.Interfaces;

public interface IWeatherService
{
    Task<WeatherAssessment> AssessActivityWeatherAsync(
        string location,
        DateOnly date,
        CancellationToken cancellationToken);

    Task<double> GetHybridRiskScoreAsync(
        string location,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken);
}
using System.Globalization;
using System.Text.Json;
using Tours.Domain.Interfaces;
using Tours.Domain.ValueObjects;

namespace Tours.Infrastructure.Weather.Services;

public sealed class OpenMeteoWeatherService(HttpClient httpClient) : IWeatherService
{
    public async Task<WeatherAssessment> AssessActivityWeatherAsync(
        string location,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var point = await ResolveCoordinatesAsync(location, cancellationToken);
        var daily = await GetDailyWeatherAsync(point.latitude, point.longitude, date, date, cancellationToken);

        if (daily.Count == 0)
        {
            return new WeatherAssessment
            {
                Date = date,
                IsSuitable = true,
                Summary = "Sin datos de clima, se mantiene actividad",
                RainProbability = 0
            };
        }

        var rain = daily[0].rainProbability;
        var isStorm = daily[0].weatherCode is >= 95 and <= 99;
        var suitable = rain < 60 && !isStorm;

        return new WeatherAssessment
        {
            Date = date,
            IsSuitable = suitable,
            RainProbability = rain,
            Summary = suitable
                ? $"Clima favorable ({rain:F0}% lluvia)"
                : $"Riesgo climático ({rain:F0}% lluvia / código {daily[0].weatherCode})"
        };
    }

    public async Task<double> GetHybridRiskScoreAsync(
        string location,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var point = await ResolveCoordinatesAsync(location, cancellationToken);

        var forecast = await GetDailyWeatherAsync(point.latitude, point.longitude, startDate, endDate, cancellationToken);
        var forecastAvg = forecast.Count == 0 ? 0 : forecast.Average(x => x.rainProbability);

        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var trailing14 = await GetDailyWeatherAsync(
            point.latitude,
            point.longitude,
            now.AddDays(-14),
            now.AddDays(-1),
            cancellationToken,
            useArchive: true);
        var trailingAvg = trailing14.Count == 0 ? 0 : trailing14.Average(x => x.rainProbability);

        var historicalScores = new List<double>(3);
        for (var yearsBack = 1; yearsBack <= 3; yearsBack++)
        {
            var histStart = startDate.AddYears(-yearsBack);
            var histEnd = endDate.AddYears(-yearsBack);
            var historical = await GetDailyWeatherAsync(
                point.latitude,
                point.longitude,
                histStart,
                histEnd,
                cancellationToken,
                useArchive: true);

            if (historical.Count > 0)
            {
                historicalScores.Add(historical.Average(x => x.rainProbability));
            }
        }

        var historicalAvg = historicalScores.Count == 0 ? 0 : historicalScores.Average();

        // Requested strategy: combine recent 14-day average + same-range 3-year historical.
        var hybridSignal = (trailingAvg + historicalAvg) / 2d;

        // Blend with direct forecast to keep near-term sensitivity.
        return (hybridSignal * 0.65d) + (forecastAvg * 0.35d);
    }

    private async Task<(double latitude, double longitude)> ResolveCoordinatesAsync(
        string location,
        CancellationToken cancellationToken)
    {
        var encoded = Uri.EscapeDataString(location);
        using var response = await httpClient.GetAsync($"https://geocoding-api.open-meteo.com/v1/search?name={encoded}&count=1&language=es&format=json", cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<GeocodePayload>(stream, cancellationToken: cancellationToken);
        var first = payload?.Results?.FirstOrDefault()
            ?? throw new InvalidOperationException($"No se pudo geolocalizar: {location}");

        return (first.Latitude, first.Longitude);
    }

    private async Task<List<(DateOnly date, double rainProbability, int weatherCode)>> GetDailyWeatherAsync(
        double latitude,
        double longitude,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken,
        bool useArchive = false)
    {
        var start = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var end = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endpoint = useArchive
            ? "https://archive-api.open-meteo.com/v1/archive"
            : "https://api.open-meteo.com/v1/forecast";

        var url =
            $"{endpoint}?latitude={latitude.ToString(CultureInfo.InvariantCulture)}" +
            $"&longitude={longitude.ToString(CultureInfo.InvariantCulture)}" +
            $"&daily=precipitation_probability_max,weather_code" +
            $"&timezone=auto&start_date={start}&end_date={end}";

        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<ForecastPayload>(stream, cancellationToken: cancellationToken);

        if (payload?.Daily?.Time is null || payload.Daily.PrecipitationProbabilityMax is null || payload.Daily.WeatherCode is null)
        {
            return [];
        }

        var size = Math.Min(payload.Daily.Time.Count, Math.Min(payload.Daily.PrecipitationProbabilityMax.Count, payload.Daily.WeatherCode.Count));
        var rows = new List<(DateOnly date, double rainProbability, int weatherCode)>(size);

        for (var i = 0; i < size; i++)
        {
            if (!DateOnly.TryParse(payload.Daily.Time[i], out var rowDate))
            {
                continue;
            }

            rows.Add((
                rowDate,
                payload.Daily.PrecipitationProbabilityMax[i],
                payload.Daily.WeatherCode[i]));
        }

        return rows;
    }

    private sealed class GeocodePayload
    {
        public List<GeocodeResult>? Results { get; set; }
    }

    private sealed class GeocodeResult
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    private sealed class ForecastPayload
    {
        public ForecastDaily? Daily { get; set; }
    }

    private sealed class ForecastDaily
    {
        public List<string>? Time { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("precipitation_probability_max")]
        public List<double>? PrecipitationProbabilityMax { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("weather_code")]
        public List<int>? WeatherCode { get; set; }
    }
}
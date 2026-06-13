using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Tours.Domain.Enums;

namespace Tours.Mcp.Tools;

[McpServerToolType]
public sealed class TourOperatorMcpTools(ToursApiClient toursApiClient)
{
    [McpServerTool(Name = "tour_operator.generate_itinerary")]
    [Description("Genera itinerario y SIEMPRE solicita datos requeridos faltantes.")]
    public async Task<object> GenerateItinerary(
        string? destination,
        string? startDate,
        string? endDate,
        int? numberOfPeople,
        List<int>? ages,
        List<string>? preferences,
        decimal? budget,
        CancellationToken cancellationToken)
    {
        var missingFields = GetMissingGenerateFields(
            destination,
            startDate,
            endDate,
            numberOfPeople,
            ages,
            preferences,
            budget);

        if (missingFields.Count != 0)
        {
            return new
            {
                status = "needs_input",
                role = "tour_operator",
                message = "Para recomendarte el mejor plan necesito los datos faltantes.",
                missingFields
            };
        }

        if (!DateOnly.TryParse(startDate, out var parsedStartDate) || !DateOnly.TryParse(endDate, out var parsedEndDate))
        {
            return new
            {
                status = "needs_input",
                role = "tour_operator",
                message = "Las fechas deben usar formato valido (ejemplo: 2026-08-10).",
                missingFields = new[] { "startDate", "endDate" }
            };
        }

        var parsedPreferences = ParsePreferences(preferences!);
        if (parsedPreferences.Count == 0)
        {
            throw new McpException("Debes indicar al menos una preferencia valida.");
        }

        var request = new GenerateItineraryApiRequest(
            Destination: destination!,
            StartDate: parsedStartDate,
            EndDate: parsedEndDate,
            NumberOfPeople: numberOfPeople!.Value,
            Ages: ages!,
            Preferences: parsedPreferences,
            Budget: budget!.Value);

        var result = await toursApiClient.GenerateItineraryAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            throw new McpException($"Error generando itinerario: {result.ErrorMessage}");
        }

        return new
        {
            status = "ok",
            role = "tour_operator",
            itinerary = result.Payload
        };
    }

    [McpServerTool(Name = "tour_operator.replace_activity")]
    [Description("Reemplaza actividad en el itinerario.")]
    public async Task<object> ReplaceActivity(
        string? itineraryId,
        string? itemId,
        string? newActivityId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(itineraryId, out var parsedItineraryId))
        {
            throw new McpException("itineraryId debe ser un GUID valido.");
        }

        if (!Guid.TryParse(itemId, out var parsedItemId))
        {
            throw new McpException("itemId debe ser un GUID valido.");
        }

        if (!Guid.TryParse(newActivityId, out var parsedNewActivityId))
        {
            throw new McpException("newActivityId debe ser un GUID valido.");
        }

        var request = new ReplaceActivityApiRequest(parsedItemId, parsedNewActivityId);
        var result = await toursApiClient.ReplaceActivityAsync(parsedItineraryId, request, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new McpException("No se encontro el itinerario o actividad.");
            }

            throw new McpException($"Error reemplazando actividad: {result.ErrorMessage}");
        }

        return new
        {
            status = "ok",
            role = "tour_operator",
            itinerary = result.Payload
        };
    }

    [McpServerTool(Name = "tour_operator.add_activity")]
    [Description("Agrega una actividad al itinerario.")]
    public async Task<object> AddActivity(
        string? itineraryId,
        string? activityId,
        string? scheduledDate,
        string? notes,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(itineraryId, out var parsedItineraryId))
        {
            throw new McpException("itineraryId debe ser un GUID valido.");
        }

        if (!Guid.TryParse(activityId, out var parsedActivityId))
        {
            throw new McpException("activityId debe ser un GUID valido.");
        }

        if (!DateOnly.TryParse(scheduledDate, out var parsedScheduledDate))
        {
            throw new McpException("scheduledDate debe ser una fecha valida (ejemplo: 2026-08-10).");
        }

        var request = new AddActivityApiRequest(
            ActivityId: parsedActivityId,
            ScheduledDate: parsedScheduledDate,
            Notes: string.IsNullOrWhiteSpace(notes) ? "Actividad extra" : notes);

        var result = await toursApiClient.AddActivityAsync(parsedItineraryId, request, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new McpException("No se encontro el itinerario o actividad.");
            }

            throw new McpException($"Error agregando actividad: {result.ErrorMessage}");
        }

        return new
        {
            status = "ok",
            role = "tour_operator",
            itinerary = result.Payload
        };
    }

    private static List<string> GetMissingGenerateFields(
        string? destination,
        string? startDate,
        string? endDate,
        int? numberOfPeople,
        List<int>? ages,
        List<string>? preferences,
        decimal? budget)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(destination))
        {
            missing.Add("destination");
        }

        if (string.IsNullOrWhiteSpace(startDate))
        {
            missing.Add("startDate");
        }

        if (string.IsNullOrWhiteSpace(endDate))
        {
            missing.Add("endDate");
        }

        if (!numberOfPeople.HasValue || numberOfPeople <= 0)
        {
            missing.Add("numberOfPeople");
        }

        if (ages is null || ages.Count == 0)
        {
            missing.Add("ages");
        }

        if (preferences is null || preferences.Count == 0)
        {
            missing.Add("preferences");
        }

        if (!budget.HasValue || budget <= 0)
        {
            missing.Add("budget");
        }

        return missing;
    }

    private static List<ActivityCategory> ParsePreferences(IEnumerable<string> values)
    {
        var parsed = new List<ActivityCategory>();

        foreach (var value in values)
        {
            if (Enum.TryParse<ActivityCategory>(value, ignoreCase: true, out var category))
            {
                parsed.Add(category);
            }
        }

        return parsed;
    }
}

public sealed record GenerateItineraryApiRequest(
    string Destination,
    DateOnly StartDate,
    DateOnly EndDate,
    int NumberOfPeople,
    IReadOnlyCollection<int> Ages,
    IReadOnlyCollection<ActivityCategory> Preferences,
    decimal Budget);

public sealed record ReplaceActivityApiRequest(
    Guid ItemId,
    Guid NewActivityId);

public sealed record AddActivityApiRequest(
    Guid ActivityId,
    DateOnly ScheduledDate,
    string Notes);

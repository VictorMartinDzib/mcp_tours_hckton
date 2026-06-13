using System.Text.Json;
using System.Text.Json.Nodes;
using Tours.Application.Dtos;
using Tours.Application.Interfaces;
using Tours.Domain.Enums;
using Tours.Mcp.Models;

namespace Tours.Mcp.Tools;

public sealed class TourOperatorMcpToolHandler(IItineraryService itineraryService) 
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<McpResponse> HandleAsync(McpRequest request, CancellationToken cancellationToken)
    {
        return request.Method switch
        {
            "tools/list" => Ok(request.Id, new
            {
                tools = new[]
                {
                    new
                    {
                        name = "tour_operator.generate_itinerary",
                        description = "Genera itinerario y SIEMPRE solicita datos requeridos faltantes.",
                        inputSchema = new
                        {
                            type = "object",
                            required = new[] { "destination", "startDate", "endDate", "numberOfPeople", "ages", "preferences", "budget" }
                        }
                    },
                    new
                    {
                        name = "tour_operator.replace_activity",
                        description = "Reemplaza actividad en el itinerario.",
                        inputSchema = new
                        {
                            type = "object",
                            required = new[] { "itineraryId", "itemId", "newActivityId" }
                        }
                    },
                    new
                    {
                        name = "tour_operator.add_activity",
                        description = "Agrega una actividad al itinerario.",
                        inputSchema = new
                        {
                            type = "object",
                            required = new[] { "itineraryId", "activityId", "scheduledDate" }
                        }
                    }
                },
                agentRules = "Actua como tour operador. Si falta informacion requerida, responde pidiendo especificamente los campos faltantes."
            }),
            "tools/call" => await HandleToolCallAsync(request, cancellationToken),
            _ => Error(request.Id, -32601, "Metodo no soportado")
        };
    }

    private async Task<McpResponse> HandleToolCallAsync(McpRequest request, CancellationToken cancellationToken)
    {
        var toolName = request.Params?["name"]?.GetValue<string>();
        var arguments = request.Params?["arguments"];

        if (string.IsNullOrWhiteSpace(toolName))
        {
            return Error(request.Id, -32602, "Falta el nombre del tool");
        }

        return toolName switch
        {
            "tour_operator.generate_itinerary" => await GenerateItineraryAsync(request.Id, arguments, cancellationToken),
            "tour_operator.replace_activity" => await ReplaceActivityAsync(request.Id, arguments, cancellationToken),
            "tour_operator.add_activity" => await AddActivityAsync(request.Id, arguments, cancellationToken),
            _ => Error(request.Id, -32601, $"Tool no soportado: {toolName}")
        };
    }

    private async Task<McpResponse> GenerateItineraryAsync(JsonNode? id, JsonNode? args, CancellationToken cancellationToken)
    {
        var requiredFields = new[]
        {
            "destination",
            "startDate",
            "endDate",
            "numberOfPeople",
            "ages",
            "preferences",
            "budget"
        };

        var missing = requiredFields
            .Where(field => args?[field] is null || string.IsNullOrWhiteSpace(args[field]?.ToString()))
            .ToArray();

        if (missing.Length != 0)
        {
            return Ok(id, new
            {
                status = "needs_input",
                role = "tour_operator",
                message = "Para recomendarte el mejor plan necesito los datos faltantes.",
                missingFields = missing
            });
        }

        try
        {
            var request = new GenerateItineraryRequest
            {
                Destination = args!["destination"]!.GetValue<string>(),
                StartDate = DateOnly.Parse(args["startDate"]!.GetValue<string>()),
                EndDate = DateOnly.Parse(args["endDate"]!.GetValue<string>()),
                NumberOfPeople = args["numberOfPeople"]!.GetValue<int>(),
                Ages = args["ages"]!.Deserialize<List<int>>(JsonOptions) ?? [],
                Preferences = ParsePreferences(args["preferences"]),
                Budget = args["budget"]!.GetValue<decimal>()
            };

            var itinerary = await itineraryService.GenerateAsync(request, cancellationToken);
            return Ok(id, new
            {
                status = "ok",
                role = "tour_operator",
                itinerary
            });
        }
        catch (Exception ex)
        {
            return Error(id, -32000, "Error generando itinerario", ex.Message);
        }
    }

    private async Task<McpResponse> ReplaceActivityAsync(JsonNode? id, JsonNode? args, CancellationToken cancellationToken)
    {
        try
        {
            var itineraryId = args?["itineraryId"]?.GetValue<Guid>() ?? Guid.Empty;
            var request = new ReplaceItineraryActivityRequest
            {
                ItemId = args?["itemId"]?.GetValue<Guid>() ?? Guid.Empty,
                NewActivityId = args?["newActivityId"]?.GetValue<Guid>() ?? Guid.Empty
            };

            var updated = await itineraryService.ReplaceActivityAsync(itineraryId, request, cancellationToken);
            return updated is null
                ? Error(id, -32001, "No se encontro el itinerario o actividad")
                : Ok(id, new { status = "ok", role = "tour_operator", itinerary = updated });
        }
        catch (Exception ex)
        {
            return Error(id, -32000, "Error reemplazando actividad", ex.Message);
        }
    }

    private async Task<McpResponse> AddActivityAsync(JsonNode? id, JsonNode? args, CancellationToken cancellationToken)
    {
        try
        {
            var itineraryId = args?["itineraryId"]?.GetValue<Guid>() ?? Guid.Empty;
            var request = new AddItineraryActivityRequest
            {
                ActivityId = args?["activityId"]?.GetValue<Guid>() ?? Guid.Empty,
                ScheduledDate = DateOnly.Parse(args?["scheduledDate"]?.GetValue<string>() ?? DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")),
                Notes = args?["notes"]?.GetValue<string>() ?? "Actividad extra"
            };

            var updated = await itineraryService.AddActivityAsync(itineraryId, request, cancellationToken);
            return updated is null
                ? Error(id, -32001, "No se encontro el itinerario o actividad")
                : Ok(id, new { status = "ok", role = "tour_operator", itinerary = updated });
        }
        catch (Exception ex)
        {
            return Error(id, -32000, "Error agregando actividad", ex.Message);
        }
    }

    private static List<ActivityCategory> ParsePreferences(JsonNode? raw)
    {
        var values = raw?.Deserialize<List<string>>(JsonOptions) ?? [];
        var result = new List<ActivityCategory>();

        foreach (var value in values)
        {
            if (Enum.TryParse<ActivityCategory>(value, ignoreCase: true, out var category))
            {
                result.Add(category);
            }
        }

        return result;
    }

    private static McpResponse Ok(JsonNode? id, object payload)
        => new()
        {
            Id = id,
            Result = JsonSerializer.SerializeToNode(payload, JsonOptions)
        };

    private static McpResponse Error(JsonNode? id, int code, string message, string? data = null)
        => new()
        {
            Id = id,
            Error = new McpError
            {
                Code = code,
                Message = message,
                Data = data is null ? null : JsonValue.Create(data)
            }
        };
}
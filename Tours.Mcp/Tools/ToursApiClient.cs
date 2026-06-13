using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Tours.Mcp.Tools;

public sealed class ToursApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<ToursApiCallResult> GenerateItineraryAsync(
        GenerateItineraryApiRequest request,
        CancellationToken cancellationToken)
        => SendAsync(HttpMethod.Post, "api/itineraries/generate", request, cancellationToken);

    public Task<ToursApiCallResult> ReplaceActivityAsync(
        Guid itineraryId,
        ReplaceActivityApiRequest request,
        CancellationToken cancellationToken)
        => SendAsync(HttpMethod.Put, $"api/itineraries/{itineraryId}/replace-activity", request, cancellationToken);

    public Task<ToursApiCallResult> AddActivityAsync(
        Guid itineraryId,
        AddActivityApiRequest request,
        CancellationToken cancellationToken)
        => SendAsync(HttpMethod.Post, $"api/itineraries/{itineraryId}/add-activity", request, cancellationToken);

    private async Task<ToursApiCallResult> SendAsync<TRequest>(
        HttpMethod method,
        string route,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(method, route)
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        return await ReadResultAsync(response, cancellationToken);
    }

    private static async Task<ToursApiCallResult> ReadResultAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var rawContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var payload = ParsePayload(rawContent);

        return new ToursApiCallResult(
            IsSuccess: response.IsSuccessStatusCode,
            StatusCode: response.StatusCode,
            Payload: payload,
            ErrorMessage: response.IsSuccessStatusCode ? null : ExtractError(payload, response.ReasonPhrase));
    }

    private static JsonNode? ParsePayload(string rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(rawContent);
        }
        catch (JsonException)
        {
            return JsonValue.Create(rawContent);
        }
    }

    private static string ExtractError(JsonNode? payload, string? fallbackReason)
    {
        if (payload is JsonValue jsonValue
            && jsonValue.TryGetValue<string>(out var messageValue)
            && !string.IsNullOrWhiteSpace(messageValue))
        {
            return messageValue;
        }

        var messageNode = payload?["message"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(messageNode))
        {
            return messageNode;
        }

        var titleNode = payload?["title"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(titleNode))
        {
            return titleNode;
        }

        return string.IsNullOrWhiteSpace(fallbackReason)
            ? "Error invocando API de Tours."
            : fallbackReason;
    }
}

public sealed record ToursApiCallResult(
    bool IsSuccess,
    HttpStatusCode StatusCode,
    JsonNode? Payload,
    string? ErrorMessage);

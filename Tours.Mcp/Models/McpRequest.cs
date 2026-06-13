using System.Text.Json.Nodes;

namespace Tours.Mcp.Models;

public sealed class McpRequest
{
    public string Jsonrpc { get; set; } = "2.0";
    public string Method { get; set; } = string.Empty;
    public JsonNode? Params { get; set; }
    public JsonNode? Id { get; set; }
}
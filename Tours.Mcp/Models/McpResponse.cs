using System.Text.Json.Nodes;

namespace Tours.Mcp.Models;

public sealed class McpResponse
{
    public string Jsonrpc { get; set; } = "2.0";
    public JsonNode? Id { get; set; }
    public JsonNode? Result { get; set; }
    public McpError? Error { get; set; }
}

public sealed class McpError
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public JsonNode? Data { get; set; }
}
using System.Text.Json;
using System.Text.Json.Nodes;
using Tours.Mcp.Models;
using Tours.Mcp.Tools;

namespace Tours.Mcp.Transport;

public static class StdioMcpServer
{
    public static async Task RunAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<TourOperatorMcpToolHandler>();

        while (true)
        {
            var line = await Console.In.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            McpRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<McpRequest>(line);
            }
            catch (Exception ex)
            {
                var parseError = new McpResponse
                {
                    Error = new McpError
                    {
                        Code = -32700,
                        Message = "JSON invalido",
                        Data = JsonValue.Create(ex.Message)
                    }
                };

                await Console.Out.WriteLineAsync(JsonSerializer.Serialize(parseError));
                await Console.Out.FlushAsync();
                continue;
            }

            if (request is null)
            {
                continue;
            }

            var response = await handler.HandleAsync(request, CancellationToken.None);
            await Console.Out.WriteLineAsync(JsonSerializer.Serialize(response));
            await Console.Out.FlushAsync();
        }
    }
}
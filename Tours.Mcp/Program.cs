using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Tours.Mcp.Tools;

var builder = WebApplication.CreateBuilder(args);
var transport = builder.Configuration["Mcp:Transport"]
	?? Environment.GetEnvironmentVariable("MCP_TRANSPORT")
	?? "http";
var isStdioTransport = string.Equals(transport, "stdio", StringComparison.OrdinalIgnoreCase);

if (isStdioTransport)
{
	builder.Logging.ClearProviders();
	builder.Logging.AddConsole(options =>
	{
		options.LogToStandardErrorThreshold = LogLevel.Trace;
	});
}

builder.Services.AddHttpClient<ToursApiClient>((serviceProvider, client) =>
{
	var configuration = serviceProvider.GetRequiredService<IConfiguration>();
	var apiBaseUrl = configuration["Mcp:ApiBaseUrl"]
		?? Environment.GetEnvironmentVariable("TOURS_API_URL")
		?? "http://localhost:5073";

	if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiBaseUri))
	{
		throw new InvalidOperationException("Mcp:ApiBaseUrl no es una URL valida.");
	}

	client.BaseAddress = apiBaseUri;
});

var mcpBuilder = builder.Services
	.AddMcpServer()
	.WithTools<TourOperatorMcpTools>();

if (isStdioTransport)
{
	mcpBuilder.WithStdioServerTransport();
}
else
{
	mcpBuilder.WithHttpTransport(options => options.Stateless = true);
}

var app = builder.Build();

if (!isStdioTransport)
{
	app.MapMcp("/mcp");
	app.MapGet("/", () => Results.Ok(new
	{
		server = "Tours MCP",
		mode = "http",
		guidance = "El agente actua como tour operador y solicita destino, fechas, personas, edades, preferencias y presupuesto."
	}));
}

await app.RunAsync();

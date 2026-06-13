using Tours.Application.Extensions;
using Tours.Infrastructure.Postgres.Extensions;
using Tours.Infrastructure.Weather.Extensions;
using Tours.Mcp.Models;
using Tours.Mcp.Tools;
using Tours.Mcp.Transport;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplication();
builder.Services.AddPostgresInfrastructure(builder.Configuration);
builder.Services.AddWeatherInfrastructure();
builder.Services.AddScoped<TourOperatorMcpToolHandler>();

var transport = builder.Configuration["Mcp:Transport"]
	?? Environment.GetEnvironmentVariable("MCP_TRANSPORT")
	?? "http";
var app = builder.Build();
await app.Services.ApplyMigrationsAndSeedAsync(CancellationToken.None);

if (string.Equals(transport, "stdio", StringComparison.OrdinalIgnoreCase))
{
	await StdioMcpServer.RunAsync(app.Services);
	return;
}

app.MapPost("/mcp", async (McpRequest request, TourOperatorMcpToolHandler handler, CancellationToken cancellationToken) =>
{
	var response = await handler.HandleAsync(request, cancellationToken);
	return Results.Json(response);
});

app.MapGet("/", () => Results.Ok(new
{
	server = "Tours MCP",
	mode = "http",
	guidance = "El agente actua como tour operador y solicita destino, fechas, personas, edades, preferencias y presupuesto."
}));

app.Run();

using Tours.Application.Extensions;
using Tours.Infrastructure.Postgres.Extensions;
using Tours.Infrastructure.Weather.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddPostgresInfrastructure(builder.Configuration);
builder.Services.AddWeatherInfrastructure();

var app = builder.Build();

await app.Services.ApplyMigrationsAndSeedAsync(CancellationToken.None);

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

using Dapper;
using System.Text.Json;
using Tours.Domain.Entities;
using Tours.Domain.Interfaces;
using Tours.Infrastructure.Postgres.Persistence;

namespace Tours.Infrastructure.Postgres.Repositories;

public sealed class ItineraryRepository(PostgresConnectionFactory connectionFactory) : IItineraryRepository
{
    public async Task<Itinerary?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = """
            select
                "Id",
                "Destination",
                "StartDate",
                "EndDate",
                "NumberOfPeople",
                "Ages",
                "Preferences",
                "Budget",
                "TotalPrice",
                "Items"
            from itineraries
            where "Id" = @Id
            limit 1;
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var row = await connection.QueryFirstOrDefaultAsync<ItineraryRow>(new CommandDefinition(
            sql,
            new { Id = id },
            cancellationToken: cancellationToken));

        return row is null ? null : ToEntity(row);
    }

    public async Task AddAsync(Itinerary itinerary, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into itineraries (
                "Id",
                "Destination",
                "StartDate",
                "EndDate",
                "NumberOfPeople",
                "Ages",
                "Preferences",
                "Budget",
                "TotalPrice",
                "Items"
            )
            values (
                @Id,
                @Destination,
                @StartDate,
                @EndDate,
                @NumberOfPeople,
                cast(@Ages as jsonb),
                cast(@Preferences as jsonb),
                @Budget,
                @TotalPrice,
                cast(@Items as jsonb)
            );
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            ToParameters(itinerary),
            cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(Itinerary itinerary, CancellationToken cancellationToken)
    {
        const string sql = """
            update itineraries
            set
                "Destination" = @Destination,
                "StartDate" = @StartDate,
                "EndDate" = @EndDate,
                "NumberOfPeople" = @NumberOfPeople,
                "Ages" = cast(@Ages as jsonb),
                "Preferences" = cast(@Preferences as jsonb),
                "Budget" = @Budget,
                "TotalPrice" = @TotalPrice,
                "Items" = cast(@Items as jsonb)
            where "Id" = @Id;
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            ToParameters(itinerary),
            cancellationToken: cancellationToken));
    }

    private static object ToParameters(Itinerary itinerary) => new
    {
        itinerary.Id,
        itinerary.Destination,
        itinerary.StartDate,
        itinerary.EndDate,
        itinerary.NumberOfPeople,
        Ages = JsonSerializer.Serialize(itinerary.Ages, JsonSerializerOptions.Default),
        Preferences = JsonSerializer.Serialize(itinerary.Preferences, JsonSerializerOptions.Default),
        itinerary.Budget,
        itinerary.TotalPrice,
        Items = JsonSerializer.Serialize(itinerary.Items, JsonSerializerOptions.Default)
    };

    private static Itinerary ToEntity(ItineraryRow row) => new()
    {
        Id = row.Id,
        Destination = row.Destination,
        StartDate = row.StartDate,
        EndDate = row.EndDate,
        NumberOfPeople = row.NumberOfPeople,
        Ages = ParseJson(row.Ages, new List<int>()),
        Preferences = ParseJson(row.Preferences, new List<Tours.Domain.Enums.ActivityCategory>()),
        Budget = row.Budget,
        TotalPrice = row.TotalPrice,
        Items = ParseJson(row.Items, new List<ItineraryItem>())
    };

    private static T ParseJson<T>(string json, T fallback)
        => string.IsNullOrWhiteSpace(json)
            ? fallback
            : JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Default) ?? fallback;

    private sealed class ItineraryRow
    {
        public Guid Id { get; init; }
        public string Destination { get; init; } = string.Empty;
        public DateOnly StartDate { get; init; }
        public DateOnly EndDate { get; init; }
        public int NumberOfPeople { get; init; }
        public string Ages { get; init; } = "[]";
        public string Preferences { get; init; } = "[]";
        public decimal Budget { get; init; }
        public decimal TotalPrice { get; init; }
        public string Items { get; init; } = "[]";
    }
}

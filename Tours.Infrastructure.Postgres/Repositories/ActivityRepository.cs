using Dapper;
using System.Text.Json;
using Tours.Domain.Entities;
using Tours.Domain.Enums;
using Tours.Domain.Interfaces;
using Tours.Infrastructure.Postgres.Persistence;

namespace Tours.Infrastructure.Postgres.Repositories;

public sealed class ActivityRepository(PostgresConnectionFactory connectionFactory) : IActivityRepository
{
    public async Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = """
            select
                "Id",
                "Name",
                "Description",
                "Price",
                "DurationMinutes",
                "Location",
                "Destination",
                "IsAvailable",
                "Reviews",
                "Photos",
                "Category",
                minimum_age as "MinimumAge",
                difficulty as "Difficulty",
                "IncludesTransport",
                "IncludesGuide",
                "ProviderName",
                "IsIndoorAlternative"
            from activities
            where "Id" = @Id
            limit 1;
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var row = await connection.QueryFirstOrDefaultAsync<ActivityRow>(new CommandDefinition(
            sql,
            new { Id = id },
            cancellationToken: cancellationToken));

        return row is null ? null : ToEntity(row);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "select count(1) from activities;",
            cancellationToken: cancellationToken));
    }

    public async Task SeedAsync(IEnumerable<Activity> activities, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into activities (
                "Id",
                "Name",
                "Description",
                "Price",
                "DurationMinutes",
                "Location",
                "Destination",
                "IsAvailable",
                "Reviews",
                "Photos",
                "Category",
                minimum_age,
                difficulty,
                "IncludesTransport",
                "IncludesGuide",
                "ProviderName",
                "IsIndoorAlternative"
            )
            values (
                @Id,
                @Name,
                @Description,
                @Price,
                @DurationMinutes,
                @Location,
                @Destination,
                @IsAvailable,
                cast(@Reviews as jsonb),
                cast(@Photos as jsonb),
                @Category,
                @MinimumAge,
                @Difficulty,
                @IncludesTransport,
                @IncludesGuide,
                @ProviderName,
                @IsIndoorAlternative
            )
            on conflict ("Id") do nothing;
            """;

        var rows = activities.Select(x => new
        {
            x.Id,
            x.Name,
            x.Description,
            x.Price,
            x.DurationMinutes,
            x.Location,
            x.Destination,
            x.IsAvailable,
            Reviews = JsonSerializer.Serialize(x.Reviews, JsonSerializerOptions.Default),
            Photos = JsonSerializer.Serialize(x.Photos, JsonSerializerOptions.Default),
            Category = (int)x.Category,
            MinimumAge = x.Requirement.MinimumAge,
            Difficulty = (int)x.Requirement.Difficulty,
            x.IncludesTransport,
            x.IncludesGuide,
            x.ProviderName,
            x.IsIndoorAlternative
        }).ToArray();

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            rows,
            cancellationToken: cancellationToken));
    }

    public async Task<List<Activity>> SearchAsync(
        string destination,
        IReadOnlyCollection<ActivityCategory> categories,
        decimal? maxPrice,
        int? minAge,
        bool onlyAvailable,
        CancellationToken cancellationToken)
    {
        var sql = """
            select
                "Id",
                "Name",
                "Description",
                "Price",
                "DurationMinutes",
                "Location",
                "Destination",
                "IsAvailable",
                "Reviews",
                "Photos",
                "Category",
                minimum_age as "MinimumAge",
                difficulty as "Difficulty",
                "IncludesTransport",
                "IncludesGuide",
                "ProviderName",
                "IsIndoorAlternative"
            from activities
            where 1 = 1
            """;

        var parameters = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(destination))
        {
            sql += """
                 and lower("Destination") = lower(@Destination)
                """;
            parameters.Add("Destination", destination.Trim());
        }

        if (categories.Count != 0)
        {
            sql += """
                 and "Category" = any(@Categories)
                """;
            parameters.Add("Categories", categories.Select(x => (int)x).ToArray());
        }

        if (maxPrice.HasValue)
        {
            sql += """
                 and "Price" <= @MaxPrice
                """;
            parameters.Add("MaxPrice", maxPrice.Value);
        }

        if (minAge.HasValue)
        {
            sql += """
                 and minimum_age <= @MinAge
                """;
            parameters.Add("MinAge", minAge.Value);
        }

        if (onlyAvailable)
        {
            sql += """
                 and "IsAvailable" = true
                """;
        }

        sql += """
             order by "Price";
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<ActivityRow>(new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken));

        return rows.Select(ToEntity).ToList();
    }

    private static Activity ToEntity(ActivityRow row) => new()
    {
        Id = row.Id,
        Name = row.Name,
        Description = row.Description,
        Price = row.Price,
        DurationMinutes = row.DurationMinutes,
        Location = row.Location,
        Destination = row.Destination,
        IsAvailable = row.IsAvailable,
        Reviews = ParseJson(row.Reviews, new List<Tours.Domain.ValueObjects.ActivityReview>()),
        Photos = ParseJson(row.Photos, new List<string>()),
        Category = (ActivityCategory)row.Category,
        Requirement = new Tours.Domain.ValueObjects.ActivityRequirement
        {
            MinimumAge = row.MinimumAge,
            Difficulty = (DifficultyLevel)row.Difficulty
        },
        IncludesTransport = row.IncludesTransport,
        IncludesGuide = row.IncludesGuide,
        ProviderName = row.ProviderName,
        IsIndoorAlternative = row.IsIndoorAlternative
    };

    private static T ParseJson<T>(string json, T fallback)
        => string.IsNullOrWhiteSpace(json)
            ? fallback
            : JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Default) ?? fallback;

    private sealed class ActivityRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public int DurationMinutes { get; init; }
        public string Location { get; init; } = string.Empty;
        public string Destination { get; init; } = string.Empty;
        public bool IsAvailable { get; init; }
        public string Reviews { get; init; } = "[]";
        public string Photos { get; init; } = "[]";
        public int Category { get; init; }
        public int MinimumAge { get; init; }
        public int Difficulty { get; init; }
        public bool IncludesTransport { get; init; }
        public bool IncludesGuide { get; init; }
        public string ProviderName { get; init; } = string.Empty;
        public bool IsIndoorAlternative { get; init; }
    }
}

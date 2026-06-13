using Dapper;
using System.Reflection;

namespace Tours.Infrastructure.Postgres.Persistence;

public sealed class PostgresMigrationRunner(PostgresConnectionFactory connectionFactory)
{
    public async Task ApplyPendingMigrationsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition("""
            create table if not exists schema_migrations (
                version text primary key,
                name text not null,
                applied_at timestamp with time zone not null default now()
            );
            """, cancellationToken: cancellationToken));

        var resources = GetMigrationScripts();
        foreach (var script in resources)
        {
            var exists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                "select count(1) from schema_migrations where version = @Version;",
                new { script.Version },
                cancellationToken: cancellationToken));

            if (exists > 0)
            {
                continue;
            }

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await connection.ExecuteAsync(new CommandDefinition(
                script.Sql,
                transaction: transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                "insert into schema_migrations (version, name) values (@Version, @Name);",
                new { script.Version, script.Name },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
        }
    }

    private static IReadOnlyCollection<MigrationScript> GetMigrationScripts()
    {
        var assembly = typeof(PostgresMigrationRunner).Assembly;
        var names = assembly.GetManifestResourceNames()
            .Where(x => x.Contains(".Migrations.Scripts.", StringComparison.Ordinal) && x.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var scripts = new List<MigrationScript>(names.Length);
        foreach (var resourceName in names)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"No se pudo leer migration script: {resourceName}");
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();
            var name = resourceName[(resourceName.LastIndexOf("Scripts.", StringComparison.Ordinal) + "Scripts.".Length)..^4];
            var separatorIndex = name.IndexOf('_');
            if (separatorIndex <= 0)
            {
                throw new InvalidOperationException($"Formato invalido de migration script: {name}");
            }

            scripts.Add(new MigrationScript(name[..separatorIndex], name, sql));
        }

        return scripts
            .OrderBy(x => x.Version, StringComparer.Ordinal)
            .ToArray();
    }

    private sealed record MigrationScript(string Version, string Name, string Sql);
}

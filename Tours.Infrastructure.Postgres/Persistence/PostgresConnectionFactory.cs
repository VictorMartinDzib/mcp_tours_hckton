using Npgsql;

namespace Tours.Infrastructure.Postgres.Persistence;

public sealed class PostgresConnectionFactory(string connectionString)
{
    public async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}

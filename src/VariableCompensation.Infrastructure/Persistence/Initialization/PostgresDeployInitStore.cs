using Npgsql;

namespace VariableCompensation.Infrastructure.Persistence.Initialization;

public sealed class PostgresDeployInitStore(string connectionString) : IDeployInitStore
{
    private const long AdvisoryLockId = 6224895494147451220L;

    public async Task RunOnceAsync(string deployId, Func<Task> initialization)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await EnsureTableAsync(connection);

        if (await IsInitializedAsync(connection, deployId))
        {
            return;
        }

        await SetLockAsync(connection, acquire: true);
        try
        {
            if (await IsInitializedAsync(connection, deployId))
            {
                return;
            }

            await initialization();
            await MarkInitializedAsync(connection, deployId);
        }
        finally
        {
            await SetLockAsync(connection, acquire: false);
        }
    }

    private static async Task EnsureTableAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS deployment_initializations (
                deployment_id text PRIMARY KEY,
                initialized_at timestamp with time zone NOT NULL DEFAULT now()
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> IsInitializedAsync(
        NpgsqlConnection connection,
        string deployId)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM deployment_initializations
                WHERE deployment_id = @deploymentId
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("deploymentId", deployId);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }

    private static async Task MarkInitializedAsync(
        NpgsqlConnection connection,
        string deployId)
    {
        const string sql = """
            INSERT INTO deployment_initializations (deployment_id)
            VALUES (@deploymentId)
            ON CONFLICT (deployment_id) DO NOTHING;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("deploymentId", deployId);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task SetLockAsync(
        NpgsqlConnection connection,
        bool acquire)
    {
        var function = acquire ? "pg_advisory_lock" : "pg_advisory_unlock";
        await using var command = new NpgsqlCommand(
            $"SELECT {function}(@lockId);",
            connection);
        command.Parameters.AddWithValue("lockId", AdvisoryLockId);
        await command.ExecuteScalarAsync();
    }
}

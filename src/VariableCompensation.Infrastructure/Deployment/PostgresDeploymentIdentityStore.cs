using Npgsql;

namespace VariableCompensation.Infrastructure.Deployment;

public sealed class PostgresDeploymentIdentityStore(string connectionString)
    : IDeploymentIdentityStore
{
    private const long AdvisoryLockId = 6224895494147451220L;

    public async Task<bool> TryAddAsync(string deploymentIdentity)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await using (var lockCommand = new NpgsqlCommand(
            "SELECT pg_advisory_xact_lock(@lockId);",
            connection,
            transaction))
        {
            lockCommand.Parameters.AddWithValue("lockId", AdvisoryLockId);
            await lockCommand.ExecuteScalarAsync();
        }

        await using (var createCommand = new NpgsqlCommand(
            """
            CREATE TABLE IF NOT EXISTS deployment_initializations (
                deployment_identity text PRIMARY KEY,
                registered_at timestamp with time zone NOT NULL DEFAULT now()
            );
            """,
            connection,
            transaction))
        {
            await createCommand.ExecuteNonQueryAsync();
        }

        await using var insertCommand = new NpgsqlCommand(
            """
            INSERT INTO deployment_initializations (deployment_identity)
            VALUES (@deploymentIdentity)
            ON CONFLICT (deployment_identity) DO NOTHING
            RETURNING deployment_identity;
            """,
            connection,
            transaction);
        insertCommand.Parameters.AddWithValue("deploymentIdentity", deploymentIdentity);

        var added = await insertCommand.ExecuteScalarAsync() is not null;
        await transaction.CommitAsync();
        return added;
    }
}

using Npgsql;

namespace VariableCompensation.Infrastructure.Deployment;

public sealed class PostgresDeploymentIdentityStore(string connectionString)
    : IDeploymentIdentityStore
{
    private const long AdvisoryLockId = 6224895494147451220L;

    public async Task<bool> RunOnceAsync(string deploymentIdentity, Func<Task> action)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Every start after the first one for a deployment ends here, without waiting for the lock.
        if (await IsRegisteredAsync(connection, transaction: null, deploymentIdentity))
        {
            return false;
        }

        // The lock is held until the action has finished, so instances that start at the same
        // time wait for it instead of serving requests against a schema that is not migrated yet.
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

        if (await IsRegisteredAsync(connection, transaction, deploymentIdentity))
        {
            await transaction.CommitAsync();
            return false;
        }

        // The deployment is registered only after the action succeeds; if it throws, the
        // transaction rolls back and the next instance runs it again.
        await action();

        await using (var insertCommand = new NpgsqlCommand(
            "INSERT INTO deployment_initializations (deployment_identity) VALUES (@deploymentIdentity);",
            connection,
            transaction))
        {
            insertCommand.Parameters.AddWithValue("deploymentIdentity", deploymentIdentity);
            await insertCommand.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        return true;
    }

    private static async Task<bool> IsRegisteredAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string deploymentIdentity)
    {
        await using var command = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM deployment_initializations WHERE deployment_identity = @deploymentIdentity);",
            connection,
            transaction);
        command.Parameters.AddWithValue("deploymentIdentity", deploymentIdentity);

        try
        {
            return await command.ExecuteScalarAsync() is true;
        }
        catch (PostgresException exception) when (
            transaction is null && exception.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return false;
        }
    }
}

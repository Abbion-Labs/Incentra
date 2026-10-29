namespace VariableCompensation.Infrastructure.Persistence.Initialization;

public interface IDbInitPolicy
{
    /// <returns><c>true</c> when <paramref name="initialize"/> ran on this start.</returns>
    Task<bool> RunAsync(Func<Task> initialize);
}

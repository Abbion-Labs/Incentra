namespace VariableCompensation.Infrastructure.Persistence.Initialization;

public sealed class OnStartDbInitPolicy : IDbInitPolicy
{
    public Task RunAsync(Func<Task> initialization) => initialization();
}

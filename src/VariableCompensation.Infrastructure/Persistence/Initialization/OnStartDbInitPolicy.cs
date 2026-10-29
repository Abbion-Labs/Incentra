namespace VariableCompensation.Infrastructure.Persistence.Initialization;

public sealed class OnStartDbInitPolicy : IDbInitPolicy
{
    public async Task<bool> RunAsync(Func<Task> initialize)
    {
        await initialize();
        return true;
    }
}

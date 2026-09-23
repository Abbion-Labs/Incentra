namespace VariableCompensation.Infrastructure.Persistence.Initialization;

public sealed class OnStartDbInitPolicy : IDbInitPolicy
{
    public Task<bool> ShouldInitAsync() => Task.FromResult(true);
}

using VariableCompensation.Infrastructure.Deployment;

namespace VariableCompensation.Infrastructure.Persistence.Initialization;

public sealed class OnDeployDbInitPolicy(
    IDeployIdProvider deployIdProvider,
    IDeployInitStore deployInitStore) : IDbInitPolicy
{
    public Task RunAsync(Func<Task> initialization) =>
        deployInitStore.RunOnceAsync(deployIdProvider.GetId(), initialization);
}

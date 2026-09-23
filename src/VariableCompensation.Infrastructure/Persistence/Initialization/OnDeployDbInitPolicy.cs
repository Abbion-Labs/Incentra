using VariableCompensation.Infrastructure.Deployment;

namespace VariableCompensation.Infrastructure.Persistence.Initialization;

public sealed class OnDeployDbInitPolicy(
    IDeploymentIdentityProvider identityProvider,
    IDeploymentIdentityStore identityStore) : IDbInitPolicy
{
    public Task<bool> ShouldInitAsync() =>
        identityStore.TryAddAsync(identityProvider.GetIdentity());
}

using Microsoft.Extensions.Logging;
using VariableCompensation.Infrastructure.Deployment;

namespace VariableCompensation.Infrastructure.Persistence.Initialization;

public sealed class OnDeployDbInitPolicy(
    IDeploymentIdentityProvider identityProvider,
    IDeploymentIdentityStore identityStore,
    ILogger<OnDeployDbInitPolicy> logger) : IDbInitPolicy
{
    public async Task<bool> RunAsync(Func<Task> initialize)
    {
        var identity = identityProvider.GetIdentity();
        if (identity is null)
        {
            logger.LogWarning(
                "The deployment identity is not available, so the database is initialized on this start.");
            await initialize();
            return true;
        }

        return await identityStore.RunOnceAsync(identity, initialize);
    }
}

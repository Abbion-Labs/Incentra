namespace VariableCompensation.Infrastructure.Deployment;

public interface IDeploymentIdentityStore
{
    Task<bool> TryAddAsync(string deploymentIdentity);
}

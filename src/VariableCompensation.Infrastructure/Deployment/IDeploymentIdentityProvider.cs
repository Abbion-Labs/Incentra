namespace VariableCompensation.Infrastructure.Deployment;

public interface IDeploymentIdentityProvider
{
    /// <returns>The identity of the running deployment, or <c>null</c> when it is not available.</returns>
    string? GetIdentity();
}

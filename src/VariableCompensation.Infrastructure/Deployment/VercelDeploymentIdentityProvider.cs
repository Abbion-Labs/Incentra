namespace VariableCompensation.Infrastructure.Deployment;

public sealed class VercelDeploymentIdentityProvider : IDeploymentIdentityProvider
{
    private const string DeploymentIdentityVariable = "VERCEL_DEPLOYMENT_ID";

    public string GetIdentity()
    {
        var identity = Environment.GetEnvironmentVariable(DeploymentIdentityVariable);
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new InvalidOperationException(
                $"{DeploymentIdentityVariable} is not available in the current environment.");
        }

        return identity;
    }
}

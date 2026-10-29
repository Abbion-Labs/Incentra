namespace VariableCompensation.Infrastructure.Deployment;

public sealed class VercelDeploymentIdentityProvider : IDeploymentIdentityProvider
{
    public const string DeploymentIdentityVariable = "VERCEL_DEPLOYMENT_ID";

    public string? GetIdentity()
    {
        var identity = Environment.GetEnvironmentVariable(DeploymentIdentityVariable);
        return string.IsNullOrWhiteSpace(identity) ? null : identity;
    }
}

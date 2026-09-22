namespace VariableCompensation.Infrastructure.Deployment;

public sealed class VercelDeployIdProvider : IDeployIdProvider
{
    private const string DeploymentIdVariable = "VERCEL_DEPLOYMENT_ID";

    public string GetId()
    {
        var deploymentId = Environment.GetEnvironmentVariable(DeploymentIdVariable);
        if (string.IsNullOrWhiteSpace(deploymentId))
        {
            throw new InvalidOperationException(
                $"{DeploymentIdVariable} is not available in the current environment.");
        }

        return deploymentId;
    }
}

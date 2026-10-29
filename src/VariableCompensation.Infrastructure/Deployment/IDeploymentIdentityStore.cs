namespace VariableCompensation.Infrastructure.Deployment;

public interface IDeploymentIdentityStore
{
    /// <summary>
    /// Runs <paramref name="action"/> unless it has already succeeded for this deployment.
    /// Concurrent callers wait until the running action has finished.
    /// </summary>
    /// <returns><c>true</c> when this call ran the action.</returns>
    Task<bool> RunOnceAsync(string deploymentIdentity, Func<Task> action);
}

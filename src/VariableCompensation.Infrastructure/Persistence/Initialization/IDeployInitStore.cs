namespace VariableCompensation.Infrastructure.Persistence.Initialization;

public interface IDeployInitStore
{
    Task RunOnceAsync(string deployId, Func<Task> initialization);
}

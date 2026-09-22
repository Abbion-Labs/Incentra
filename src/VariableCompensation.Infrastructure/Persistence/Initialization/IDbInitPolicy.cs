namespace VariableCompensation.Infrastructure.Persistence.Initialization;

public interface IDbInitPolicy
{
    Task RunAsync(Func<Task> initialization);
}

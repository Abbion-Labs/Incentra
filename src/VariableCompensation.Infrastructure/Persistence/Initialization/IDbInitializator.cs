namespace VariableCompensation.Infrastructure.Persistence.Initialization;

public interface IDbInitializator
{
    Task InitializeAsync();
}

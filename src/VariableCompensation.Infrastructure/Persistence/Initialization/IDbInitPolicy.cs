namespace VariableCompensation.Infrastructure.Persistence.Initialization;

public interface IDbInitPolicy
{
    Task<bool> ShouldInitAsync();
}

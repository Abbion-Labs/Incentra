namespace VariableCompensation.Infrastructure.Persistence.Initialization;

public interface IDatabaseSeeder
{
    Task SeedAsync();
}

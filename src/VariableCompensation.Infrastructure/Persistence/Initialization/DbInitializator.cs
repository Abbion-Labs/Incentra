using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VariableCompensation.Infrastructure.Persistence;

namespace VariableCompensation.Infrastructure.Persistence.Initialization;

public sealed class DbInitializator(
    IDbInitPolicy policy,
    AppDbContext context,
    IDatabaseSeeder seeder,
    IOptions<DbInitOptions> options) : IDbInitializator
{
    public async Task InitializeAsync()
    {
        if (!await policy.ShouldInitAsync())
        {
            return;
        }

        await context.Database.MigrateAsync();

        if (options.Value.Seed)
        {
            await seeder.SeedAsync();
        }
    }
}

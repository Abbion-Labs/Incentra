using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VariableCompensation.Infrastructure.Persistence;

namespace VariableCompensation.Infrastructure.Persistence.Initialization;

public sealed class DbInitializator(
    IDbInitPolicy policy,
    AppDbContext context,
    IDatabaseSeeder seeder,
    IOptions<DbInitOptions> options,
    ILogger<DbInitializator> logger) : IDbInitializator
{
    public async Task InitializeAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        if (await policy.RunAsync(MigrateAndSeedAsync))
        {
            logger.LogInformation(
                "Database initialized in {ElapsedMilliseconds} ms",
                stopwatch.ElapsedMilliseconds);
        }
        else
        {
            logger.LogInformation(
                "Database initialization skipped, it already ran for this deployment ({ElapsedMilliseconds} ms)",
                stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task MigrateAndSeedAsync()
    {
        await context.Database.MigrateAsync();

        if (options.Value.Seed)
        {
            await seeder.SeedAsync();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VariableCompensation.Application;
using VariableCompensation.Application.Abstractions.Security;
using VariableCompensation.Application.Abstractions.Storage;
using VariableCompensation.Infrastructure.Persistence;
using VariableCompensation.Infrastructure.Storage;

namespace VariableCompensation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();
        services.AddSingleton<IEmployeeAvatarStorage, LocalEmployeeAvatarStorage>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddAuthInfrastructure(configuration);

        return services;
    }

    public static async Task MigrateAndSeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var encryption = scope.ServiceProvider.GetRequiredService<ISensitiveDataEncryptionService>();
        await DatabaseSeeder.SeedAsync(context, encryption);
    }
}

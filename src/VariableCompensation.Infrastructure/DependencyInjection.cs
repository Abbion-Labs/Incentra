using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VariableCompensation.Application;
using VariableCompensation.Application.Abstractions.Security;
using VariableCompensation.Application.Abstractions.Storage;
using VariableCompensation.Infrastructure.Deployment;
using VariableCompensation.Infrastructure.Persistence;
using VariableCompensation.Infrastructure.Persistence.Initialization;
using VariableCompensation.Infrastructure.Storage;

namespace VariableCompensation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();
        AddEmployeeAvatarStorage(services, configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        AddDatabaseInitialization(services, configuration, connectionString);
        services.AddAuthInfrastructure(configuration);

        return services;
    }

    public static async Task MigrateAndSeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var policy = serviceProvider.GetRequiredService<IDbInitPolicy>();
        var context = serviceProvider.GetRequiredService<AppDbContext>();
        var encryption = serviceProvider.GetRequiredService<ISensitiveDataEncryptionService>();

        await policy.RunAsync(async () =>
        {
            await MigrateDatabaseAsync(context);
            await SeedDatabaseAsync(context, encryption);
        });
    }

    private static Task MigrateDatabaseAsync(AppDbContext context) =>
        context.Database.MigrateAsync();

    private static Task SeedDatabaseAsync(
        AppDbContext context,
        ISensitiveDataEncryptionService encryption) =>
        DatabaseSeeder.SeedAsync(context, encryption);

    private static void AddDatabaseInitialization(
        IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        var policyName = configuration["DbInit:Policy"] ?? "OnStart";

        services.AddSingleton<IDeployIdProvider, VercelDeployIdProvider>();
        services.AddSingleton<IDeployInitStore>(
            _ => new PostgresDeployInitStore(connectionString));
        services.AddScoped<OnStartDbInitPolicy>();
        services.AddScoped<OnDeployDbInitPolicy>();
        services.AddScoped<IDbInitPolicy>(serviceProvider => policyName switch
        {
            "OnStart" => serviceProvider.GetRequiredService<OnStartDbInitPolicy>(),
            "OnDeploy" => serviceProvider.GetRequiredService<OnDeployDbInitPolicy>(),
            _ => throw new InvalidOperationException(
                $"Unsupported DbInit policy '{policyName}'. Expected 'OnStart' or 'OnDeploy'."),
        });
    }

    private static void AddEmployeeAvatarStorage(IServiceCollection services, IConfiguration configuration)
    {
        var storageOptions = configuration
            .GetSection(EmployeeAvatarStorageOptions.SectionName)
            .Get<EmployeeAvatarStorageOptions>() ?? new EmployeeAvatarStorageOptions();

        services.Configure<EmployeeAvatarStorageOptions>(
            configuration.GetSection(EmployeeAvatarStorageOptions.SectionName));

        if (string.Equals(
            storageOptions.Provider,
            EmployeeAvatarStorageOptions.LocalProvider,
            StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IEmployeeAvatarStorage, LocalEmployeeAvatarStorage>();
            return;
        }

        if (!string.Equals(
            storageOptions.Provider,
            EmployeeAvatarStorageOptions.SupabaseProvider,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported avatar storage provider '{storageOptions.Provider}'. "
                + $"Expected '{EmployeeAvatarStorageOptions.LocalProvider}' or '{EmployeeAvatarStorageOptions.SupabaseProvider}'.");
        }

        var supabaseOptions = configuration
            .GetSection(SupabaseEmployeeAvatarStorageOptions.SectionName)
            .Get<SupabaseEmployeeAvatarStorageOptions>() ?? new SupabaseEmployeeAvatarStorageOptions();

        ValidateSupabaseStorageOptions(supabaseOptions);

        services.Configure<SupabaseEmployeeAvatarStorageOptions>(
            configuration.GetSection(SupabaseEmployeeAvatarStorageOptions.SectionName));

        services.AddHttpClient(
            SupabaseEmployeeAvatarStorage.HttpClientName,
            client =>
            {
                client.BaseAddress = new Uri($"{supabaseOptions.Url.TrimEnd('/')}/");
                client.DefaultRequestHeaders.Add("apikey", supabaseOptions.SecretKey);
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", supabaseOptions.SecretKey);
            });

        services.AddSingleton<IEmployeeAvatarStorage, SupabaseEmployeeAvatarStorage>();
    }

    private static void ValidateSupabaseStorageOptions(SupabaseEmployeeAvatarStorageOptions options)
    {
        if (!Uri.TryCreate(options.Url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException(
                $"{SupabaseEmployeeAvatarStorageOptions.SectionName}:Url must be an absolute HTTP(S) URL.");
        }

        if (string.IsNullOrWhiteSpace(options.SecretKey))
        {
            throw new InvalidOperationException(
                $"{SupabaseEmployeeAvatarStorageOptions.SectionName}:SecretKey is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Bucket))
        {
            throw new InvalidOperationException(
                $"{SupabaseEmployeeAvatarStorageOptions.SectionName}:Bucket is not configured.");
        }
    }
}

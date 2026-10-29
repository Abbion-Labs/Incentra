using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VariableCompensation.Application;
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

        AddDeployment(services, configuration, connectionString);
        AddDatabaseInitialization(services, configuration);
        services.AddAuthInfrastructure(configuration);

        return services;
    }

    private static void AddDeployment(
        IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        var section = configuration.GetSection(DeploymentOptions.SectionName);
        var options = section.Get<DeploymentOptions>() ?? new DeploymentOptions();

        services.Configure<DeploymentOptions>(section);

        services.AddKeyedSingleton<
            IDeploymentIdentityProvider,
            VercelDeploymentIdentityProvider>(DeploymentOptions.VercelIdentityProvider);

        services.AddKeyedSingleton<IDeploymentIdentityStore>(
            DeploymentOptions.PostgresIdentityStore,
            (_, _) => new PostgresDeploymentIdentityStore(connectionString));

        services.AddSingleton<IDeploymentIdentityProvider>(
            serviceProvider => serviceProvider.GetRequiredKeyedService<IDeploymentIdentityProvider>(
                options.IdentityProvider));

        services.AddSingleton<IDeploymentIdentityStore>(
            serviceProvider => serviceProvider.GetRequiredKeyedService<IDeploymentIdentityStore>(
                options.IdentityStore));
    }

    private static void AddDatabaseInitialization(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(DbInitOptions.SectionName);
        var options = section.Get<DbInitOptions>() ?? new DbInitOptions();

        services.Configure<DbInitOptions>(section);

        services.AddScoped<IDbInitializator, DbInitializator>();
        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

        services.AddKeyedScoped<IDbInitPolicy, OnStartDbInitPolicy>(DbInitOptions.OnStartPolicy);
        services.AddKeyedScoped<IDbInitPolicy, OnDeployDbInitPolicy>(DbInitOptions.OnDeployPolicy);
        services.AddScoped<IDbInitPolicy>(
            serviceProvider => serviceProvider.GetRequiredKeyedService<IDbInitPolicy>(options.Policy));
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

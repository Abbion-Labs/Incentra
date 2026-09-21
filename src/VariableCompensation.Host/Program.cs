using Serilog;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using VariableCompensation.Api;
using VariableCompensation.Host.Middleware;
using VariableCompensation.Infrastructure;
using VariableCompensation.Infrastructure.Storage;

const string migrateArgument = "--migrate";
var migrateAndExit = args.Any(argument =>
    string.Equals(argument, migrateArgument, StringComparison.OrdinalIgnoreCase));
var hostArguments = args
    .Where(argument => !string.Equals(argument, migrateArgument, StringComparison.OrdinalIgnoreCase))
    .ToArray();

var builder = WebApplication.CreateBuilder(hostArguments);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

if (migrateAndExit)
{
    await VariableCompensation.Infrastructure.DependencyInjection.MigrateDatabaseAsync(app.Services);
    await VariableCompensation.Infrastructure.DependencyInjection.SeedDatabaseAsync(app.Services);
    return;
}

var avatarStorageProvider =
    builder.Configuration[$"{EmployeeAvatarStorageOptions.SectionName}:Provider"]
    ?? EmployeeAvatarStorageOptions.LocalProvider;

if (string.Equals(
    avatarStorageProvider,
    EmployeeAvatarStorageOptions.LocalProvider,
    StringComparison.OrdinalIgnoreCase))
{
    var avatarDirectory = LocalEmployeeAvatarStorage.AvatarsDirectory(app.Environment);
    Directory.CreateDirectory(avatarDirectory);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(avatarDirectory),
        RequestPath = "/avatars",
    });
}

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.MapHealthChecks("/health");
app.MapApiEndpoints();

app.Run();

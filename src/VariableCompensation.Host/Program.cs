using Serilog;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using VariableCompensation.Api;
using VariableCompensation.Host.Middleware;
using VariableCompensation.Infrastructure;
using VariableCompensation.Infrastructure.Persistence.Initialization;
using VariableCompensation.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

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

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbInitializator = scope.ServiceProvider.GetRequiredService<IDbInitializator>();
    await dbInitializator.InitializeAsync();
}

app.Run();

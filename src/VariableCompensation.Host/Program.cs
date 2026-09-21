using System.Diagnostics;
using Serilog;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using VariableCompensation.Api;
using VariableCompensation.Host.Middleware;
using VariableCompensation.Infrastructure;
using VariableCompensation.Infrastructure.Storage;

var startupTimer = Stopwatch.StartNew();
Console.WriteLine("[StartupProfile] Program start");

var builder = WebApplication.CreateBuilder(args);
Console.WriteLine($"[StartupProfile] CreateBuilder completed at {startupTimer.ElapsedMilliseconds} ms");

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
Console.WriteLine($"[StartupProfile] Service registration completed at {startupTimer.ElapsedMilliseconds} ms");

var buildTimer = Stopwatch.StartNew();
var app = builder.Build();
Console.WriteLine($"[StartupProfile] WebApplication.Build took {buildTimer.ElapsedMilliseconds} ms; total {startupTimer.ElapsedMilliseconds} ms");

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
Console.WriteLine($"[StartupProfile] HTTP pipeline configured at {startupTimer.ElapsedMilliseconds} ms");

if (!app.Environment.IsEnvironment("Testing"))
{
    var databaseStartupTimer = Stopwatch.StartNew();
    Console.WriteLine($"[StartupProfile] MigrateAndSeed starting at {startupTimer.ElapsedMilliseconds} ms");
    await VariableCompensation.Infrastructure.DependencyInjection.MigrateAndSeedAsync(app.Services);
    Console.WriteLine($"[StartupProfile] MigrateAndSeed took {databaseStartupTimer.ElapsedMilliseconds} ms; total {startupTimer.ElapsedMilliseconds} ms");
}

Console.WriteLine($"[StartupProfile] Application ready to run at {startupTimer.ElapsedMilliseconds} ms");
app.Run();

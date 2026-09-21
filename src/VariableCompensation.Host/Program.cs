using System.Diagnostics;
using Serilog;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using VariableCompensation.Api;
using VariableCompensation.Host.Middleware;
using VariableCompensation.Infrastructure;
using VariableCompensation.Infrastructure.Storage;

var process = Process.GetCurrentProcess();
var processStartUtc = process.StartTime.ToUniversalTime();
var programEnteredUtc = DateTime.UtcNow;
var processToProgramMs = (programEnteredUtc - processStartUtc).TotalMilliseconds;
var startupTimer = Stopwatch.StartNew();

Console.WriteLine(
    $"[StartupProfile] Process started at {processStartUtc:O}; Program entered at {programEnteredUtc:O}; process-to-Program {processToProgramMs:F0} ms");

var stageTimer = Stopwatch.StartNew();
var builder = WebApplication.CreateBuilder(args);
Console.WriteLine($"[StartupProfile] CreateBuilder took {stageTimer.ElapsedMilliseconds} ms; total {startupTimer.ElapsedMilliseconds} ms");

stageTimer.Restart();
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));
Console.WriteLine($"[StartupProfile] Serilog host configuration took {stageTimer.ElapsedMilliseconds} ms; total {startupTimer.ElapsedMilliseconds} ms");

stageTimer.Restart();
builder.Services.AddInfrastructure(builder.Configuration);
Console.WriteLine($"[StartupProfile] AddInfrastructure took {stageTimer.ElapsedMilliseconds} ms; total {startupTimer.ElapsedMilliseconds} ms");

stageTimer.Restart();
builder.Services.AddApi();
Console.WriteLine($"[StartupProfile] AddApi took {stageTimer.ElapsedMilliseconds} ms; total {startupTimer.ElapsedMilliseconds} ms");

stageTimer.Restart();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
Console.WriteLine($"[StartupProfile] Remaining service registration took {stageTimer.ElapsedMilliseconds} ms; total {startupTimer.ElapsedMilliseconds} ms");

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

var firstRequestSeen = 0;
app.Use(async (context, next) =>
{
    if (Interlocked.Exchange(ref firstRequestSeen, 1) == 0)
    {
        Console.WriteLine(
            $"[StartupProfile] First request entered ASP.NET pipeline at {DateTime.UtcNow:O}; total since Program {startupTimer.ElapsedMilliseconds} ms");
    }

    await next();
});

Console.WriteLine($"[StartupProfile] HTTP pipeline configured at {startupTimer.ElapsedMilliseconds} ms");

if (!app.Environment.IsEnvironment("Testing"))
{
    var databaseStartupTimer = Stopwatch.StartNew();
    Console.WriteLine($"[StartupProfile] MigrateAndSeed starting at {startupTimer.ElapsedMilliseconds} ms");
    await VariableCompensation.Infrastructure.DependencyInjection.MigrateAndSeedAsync(app.Services);
    Console.WriteLine($"[StartupProfile] MigrateAndSeed took {databaseStartupTimer.ElapsedMilliseconds} ms; total {startupTimer.ElapsedMilliseconds} ms");
}

Console.WriteLine($"[StartupProfile] Calling app.Run at {DateTime.UtcNow:O}; total since Program {startupTimer.ElapsedMilliseconds} ms");

app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine(
        $"[StartupProfile] ApplicationStarted fired at {DateTime.UtcNow:O}; total since Program {startupTimer.ElapsedMilliseconds} ms");
});

app.Run();

using Serilog;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using VariableCompensation.Api;
using VariableCompensation.Infrastructure;
using VariableCompensation.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

var avatarDirectory = LocalEmployeeAvatarStorage.AvatarsDirectory(app.Environment);
Directory.CreateDirectory(avatarDirectory);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(avatarDirectory),
    RequestPath = "/avatars",
});

app.UseSerilogRequestLogging();
app.MapHealthChecks("/health");
app.MapApiEndpoints();

if (!app.Environment.IsEnvironment("Testing"))
{
    await VariableCompensation.Infrastructure.DependencyInjection.MigrateAndSeedAsync(app.Services);
}

app.Run();

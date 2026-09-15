using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VariableCompensation.Application.Abstractions.Security;
using VariableCompensation.Infrastructure.Persistence;
using VariableCompensation.Testing.Common.Seeding;

namespace VariableCompensation.Integration.Tests.Infrastructure;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlFixture postgres;
    private bool seeded;

    public CustomWebApplicationFactory(PostgreSqlFixture postgres)
    {
        this.postgres = postgres;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(this.postgres.ConnectionString));
        });

        builder.ConfigureServices(services =>
        {
            if (this.seeded)
            {
                return;
            }

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var encryption = scope.ServiceProvider.GetRequiredService<ISensitiveDataEncryptionService>();
            TestDataSeeder.SeedAsync(context, encryption).GetAwaiter().GetResult();
            this.seeded = true;
        });
    }
}

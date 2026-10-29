using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using VariableCompensation.Infrastructure.Deployment;
using VariableCompensation.Infrastructure.Persistence;
using VariableCompensation.Infrastructure.Persistence.Initialization;

namespace VariableCompensation.Integration.Tests.DatabaseInitialization;

/// <summary>
/// On Vercel the database is migrated and seeded once per deployment: the first instance does it while the
/// others wait, and a failed attempt is repeated by the next instance.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class DatabaseInitializationIntegrationTests
{
    private readonly IntegrationTestFixture fixture;

    public DatabaseInitializationIntegrationTests(IntegrationTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Store_RunsTheActionOnlyOnceForADeployment()
    {
        var store = new PostgresDeploymentIdentityStore(this.fixture.ConnectionString);
        var deployment = UniqueDeployment();
        var runs = 0;

        var first = await store.RunOnceAsync(deployment, () => { runs++; return Task.CompletedTask; });
        var second = await store.RunOnceAsync(deployment, () => { runs++; return Task.CompletedTask; });

        first.Should().BeTrue();
        second.Should().BeFalse();
        runs.Should().Be(1);
    }

    [Fact]
    public async Task Store_RunsTheActionForEachDeployment()
    {
        var store = new PostgresDeploymentIdentityStore(this.fixture.ConnectionString);

        var first = await store.RunOnceAsync(UniqueDeployment(), () => Task.CompletedTask);
        var second = await store.RunOnceAsync(UniqueDeployment(), () => Task.CompletedTask);

        first.Should().BeTrue();
        second.Should().BeTrue();
    }

    [Fact]
    public async Task Store_InstanceStartingDuringInitializationWaitsForItAndThenSkips()
    {
        var store = new PostgresDeploymentIdentityStore(this.fixture.ConnectionString);
        var deployment = UniqueDeployment();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runs = 0;

        var first = store.RunOnceAsync(deployment, async () =>
        {
            runs++;
            started.SetResult();
            await release.Task;
        });
        await started.Task;

        var second = store.RunOnceAsync(deployment, () => { runs++; return Task.CompletedTask; });
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        second.IsCompleted.Should().BeFalse("the second instance must wait until the first one has finished");

        release.SetResult();

        (await first).Should().BeTrue();
        (await second).Should().BeFalse();
        runs.Should().Be(1);
    }

    [Fact]
    public async Task Store_RunsTheActionAgainAfterItFailed()
    {
        var store = new PostgresDeploymentIdentityStore(this.fixture.ConnectionString);
        var deployment = UniqueDeployment();

        var failed = () => store.RunOnceAsync(
            deployment,
            () => throw new InvalidOperationException("Migration failed."));
        await failed.Should().ThrowAsync<InvalidOperationException>();

        var retried = await store.RunOnceAsync(deployment, () => Task.CompletedTask);

        retried.Should().BeTrue();
    }

    [Fact]
    public async Task Store_WorksOnADatabaseThatHasNoDeploymentsYet()
    {
        var connectionString = await this.CreateEmptyDatabaseAsync();
        var store = new PostgresDeploymentIdentityStore(connectionString);
        var deployment = UniqueDeployment();

        var first = await store.RunOnceAsync(deployment, () => Task.CompletedTask);
        var second = await store.RunOnceAsync(deployment, () => Task.CompletedTask);

        first.Should().BeTrue();
        second.Should().BeFalse();
    }

    [Fact]
    public async Task OnDeployPolicy_InitializesThroughTheStoreForTheDeployment()
    {
        var store = new RecordingStore();
        var policy = new OnDeployDbInitPolicy(
            new FixedIdentityProvider("dpl_test"),
            store,
            NullLogger<OnDeployDbInitPolicy>.Instance);

        var initialized = await policy.RunAsync(() => Task.CompletedTask);

        initialized.Should().BeTrue();
        store.Deployments.Should().Equal("dpl_test");
    }

    [Fact]
    public async Task OnDeployPolicy_InitializesOnEveryStartWithoutADeploymentIdentity()
    {
        var store = new RecordingStore();
        var policy = new OnDeployDbInitPolicy(
            new FixedIdentityProvider(null),
            store,
            NullLogger<OnDeployDbInitPolicy>.Instance);
        var runs = 0;

        var initialized = await policy.RunAsync(() => { runs++; return Task.CompletedTask; });

        initialized.Should().BeTrue();
        runs.Should().Be(1);
        store.Deployments.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task Initializator_SeedsOnlyWhenSeedIsTurnedOn(bool seed, int expectedSeeds)
    {
        await using var context = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(this.fixture.ConnectionString).Options);
        var seeder = new CountingSeeder();
        var initializator = new DbInitializator(
            new OnStartDbInitPolicy(),
            context,
            seeder,
            Options.Create(new DbInitOptions { Seed = seed }),
            NullLogger<DbInitializator>.Instance);

        await initializator.InitializeAsync();

        seeder.Seeds.Should().Be(expectedSeeds);
    }

    [Fact]
    public async Task Initializator_DoesNothingWhenThePolicySkipsInitialization()
    {
        await using var context = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(this.fixture.ConnectionString).Options);
        var seeder = new CountingSeeder();
        var initializator = new DbInitializator(
            new SkippingPolicy(),
            context,
            seeder,
            Options.Create(new DbInitOptions { Seed = true }),
            NullLogger<DbInitializator>.Instance);

        await initializator.InitializeAsync();

        seeder.Seeds.Should().Be(0);
    }

    private static string UniqueDeployment() => $"dpl_{Guid.NewGuid():N}";

    private async Task<string> CreateEmptyDatabaseAsync()
    {
        var database = $"deployments_{Guid.NewGuid():N}";
        await using (var connection = new NpgsqlConnection(this.fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE {database};", connection);
            await command.ExecuteNonQueryAsync();
        }

        return new NpgsqlConnectionStringBuilder(this.fixture.ConnectionString) { Database = database }
            .ConnectionString;
    }

    private sealed class FixedIdentityProvider(string? identity) : IDeploymentIdentityProvider
    {
        public string? GetIdentity() => identity;
    }

    private sealed class RecordingStore : IDeploymentIdentityStore
    {
        public List<string> Deployments { get; } = [];

        public async Task<bool> RunOnceAsync(string deploymentIdentity, Func<Task> action)
        {
            this.Deployments.Add(deploymentIdentity);
            await action();
            return true;
        }
    }

    private sealed class SkippingPolicy : IDbInitPolicy
    {
        public Task<bool> RunAsync(Func<Task> initialize) => Task.FromResult(false);
    }

    private sealed class CountingSeeder : IDatabaseSeeder
    {
        public int Seeds { get; private set; }

        public Task SeedAsync()
        {
            this.Seeds++;
            return Task.CompletedTask;
        }
    }
}

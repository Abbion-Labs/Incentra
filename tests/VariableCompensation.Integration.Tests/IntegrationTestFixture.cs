using Microsoft.AspNetCore.Mvc.Testing;
using VariableCompensation.Integration.Tests.Infrastructure;

namespace VariableCompensation.Integration.Tests;

[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationTestFixture>;

public sealed class IntegrationTestFixture : IAsyncLifetime, IDisposable
{
    private readonly PostgreSqlFixture postgres = new();

    public CustomWebApplicationFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await this.postgres.InitializeAsync();
        this.Factory = new CustomWebApplicationFactory(this.postgres);
    }

    public Task DisposeAsync() => this.postgres.DisposeAsync();

    public void Dispose() => this.Factory?.Dispose();
}

using Testcontainers.PostgreSql;

namespace VariableCompensation.Integration.Tests.Infrastructure;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("variable_compensation_test")
        .WithUsername("vc_test")
        .WithPassword("vc_test_password")
        .Build();

    public string ConnectionString => this.container.GetConnectionString();

    public Task InitializeAsync() => this.container.StartAsync();

    public Task DisposeAsync() => this.container.DisposeAsync().AsTask();
}

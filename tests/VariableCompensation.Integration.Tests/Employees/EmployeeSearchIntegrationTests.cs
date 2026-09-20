using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Employees;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class EmployeeSearchIntegrationTests
{
    private readonly IntegrationTestFixture fixture;

    public EmployeeSearchIntegrationTests(IntegrationTestFixture fixture)
    {
        this.fixture = fixture;
    }

    // The seeded employee is Marko Marković.
    [Theory]
    [InlineData("Marko")]
    [InlineData("Marković")]
    [InlineData("Marko M")]
    [InlineData("Marko Marković")]
    [InlineData("Marković Marko")]
    [InlineData("  marko   marković ")]
    public async Task Search_FindsTheEmployeeByAnyPartOfTheFullName(string search)
    {
        var ids = await this.SearchIdsAsync(search);

        ids.Should().Contain(TestEmployeeIds.Employee);
    }

    [Fact]
    public async Task Search_WithATermThatMatchesNobody_ReturnsNothing()
    {
        var ids = await this.SearchIdsAsync("Marko Jovanović");

        ids.Should().BeEmpty();
    }

    private async Task<IReadOnlyList<long>> SearchIdsAsync(string search)
    {
        var client = await this.CreateClientAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);

        var response = await client.GetFromJsonAsync<JsonElement>(
            $"/api/employees?page=1&pageSize=100&search={Uri.EscapeDataString(search)}");

        return response.GetProperty("items").EnumerateArray()
            .Select(e => e.GetProperty("id").GetInt64())
            .ToList();
    }

    private async Task<HttpClient> CreateClientAsync(string email, string password)
    {
        var client = this.fixture.Factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Evaluations;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class RoleWorkspaceIntegrationTests
{
    private readonly IntegrationTestFixture fixture;

    public RoleWorkspaceIntegrationTests(IntegrationTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Evaluator_ListsOnlyTheirOwnEmployeesAndEvaluations()
    {
        var client = await this.CreateClientAsync(TestCredentials.EvaluatorEmail, TestCredentials.EvaluatorPassword);

        var employees = await client.GetFromJsonAsync<JsonElement>("/api/evaluator/employees?page=1&pageSize=100");
        var evaluations = await client.GetFromJsonAsync<JsonElement>("/api/evaluator/evaluations?page=1&pageSize=100");

        EvaluatorIds(employees).Should().NotBeEmpty().And.OnlyContain(id => id == TestEmployeeIds.Evaluator);
        EvaluatorIds(evaluations).Should().OnlyContain(id => id == TestEmployeeIds.Evaluator);
    }

    [Fact]
    public async Task Controller_ListsEmployeesOfTheEvaluatorsTheySupervise()
    {
        var client = await this.CreateClientAsync(TestCredentials.ControllerEmail, TestCredentials.ControllerPassword);

        var employees = await client.GetFromJsonAsync<JsonElement>("/api/controller/employees?page=1&pageSize=100");
        var analytics = await client.GetAsync($"/api/controller/evaluators/{TestEmployeeIds.Evaluator}/analytics");

        EvaluatorIds(employees).Should().NotBeEmpty().And.OnlyContain(id => id == TestEmployeeIds.Evaluator);
        analytics.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Admin_WithoutTheEvaluatorRole_SeesEveryoneOnTheEvaluatorList()
    {
        var client = await this.CreateClientAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);

        var employees = await client.GetFromJsonAsync<JsonElement>("/api/evaluator/employees?page=1&pageSize=100");

        EvaluatorIds(employees).Should().Contain(id => id != TestEmployeeIds.Evaluator);
    }

    [Theory]
    [InlineData(TestCredentials.EvaluatorEmail, TestCredentials.EvaluatorPassword, "/api/controller/employees")]
    [InlineData(TestCredentials.EvaluatorEmail, TestCredentials.EvaluatorPassword, "/api/controller/evaluations")]
    [InlineData(TestCredentials.ControllerEmail, TestCredentials.ControllerPassword, "/api/evaluator/employees")]
    [InlineData(TestCredentials.ControllerEmail, TestCredentials.ControllerPassword, "/api/evaluator/evaluations")]
    [InlineData(TestCredentials.EmployeeEmail, TestCredentials.EmployeePassword, "/api/evaluator/evaluations")]
    public async Task RoleLists_AreClosedToUsersWithoutThatRole(string email, string password, string path)
    {
        var client = await this.CreateClientAsync(email, password);

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static IEnumerable<long?> EvaluatorIds(JsonElement page) =>
        page.GetProperty("items").EnumerateArray().Select(item =>
            item.GetProperty("evaluatorEmployeeId").ValueKind == JsonValueKind.Null
                ? (long?)null
                : item.GetProperty("evaluatorEmployeeId").GetInt64());

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

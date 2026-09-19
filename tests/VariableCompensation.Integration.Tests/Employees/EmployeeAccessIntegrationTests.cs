using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Employees;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class EmployeeAccessIntegrationTests
{
    private readonly IntegrationTestFixture fixture;

    public EmployeeAccessIntegrationTests(IntegrationTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Employee_ListsOnlyOwnRecord()
    {
        var client = await this.CreateClientAsync(TestCredentials.EmployeeEmail, TestCredentials.EmployeePassword);

        var response = await client.GetFromJsonAsync<JsonElement>("/api/employees?page=1&pageSize=100");

        response.GetProperty("items").EnumerateArray().Select(e => e.GetProperty("id").GetInt64())
            .Should().Equal(TestEmployeeIds.Employee);
    }

    [Fact]
    public async Task Employee_CanReadOwnRecord()
    {
        var client = await this.CreateClientAsync(TestCredentials.EmployeeEmail, TestCredentials.EmployeePassword);

        var response = await client.GetAsync($"/api/employees/{TestEmployeeIds.Employee}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Employee_CannotReadColleagueById()
    {
        var client = await this.CreateClientAsync(TestCredentials.EmployeeEmail, TestCredentials.EmployeePassword);

        var response = await client.GetAsync($"/api/employees/{TestEmployeeIds.Evaluator}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("error").GetString().Should().Be("vn-0069");
    }

    [Fact]
    public async Task Admin_CanReadAnyEmployeeById()
    {
        var client = await this.CreateClientAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);

        var response = await client.GetAsync($"/api/employees/{TestEmployeeIds.Evaluator}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Payroll_SeesNoEmployeesAndNoEvaluations()
    {
        var client = await this.CreateClientAsync(TestCredentials.PayrollEmail, TestCredentials.PayrollPassword);

        var employees = await client.GetFromJsonAsync<JsonElement>("/api/employees?page=1&pageSize=100");
        var evaluations = await client.GetFromJsonAsync<JsonElement>("/api/evaluations?page=1&pageSize=100");

        employees.GetProperty("items").GetArrayLength().Should().Be(0);
        evaluations.GetProperty("items").GetArrayLength().Should().Be(0);
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

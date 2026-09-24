using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using VariableCompensation.Domain;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Auth;

/// <summary>
/// An account with a role that acts as an employee (employee, evaluator, controller) belongs to an employee from the
/// moment it is created, and stays with that employee.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class AccountEmployeeLinkIntegrationTests
{
    private readonly IntegrationTestFixture fixture;

    public AccountEmployeeLinkIntegrationTests(IntegrationTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task AnEvaluatorAccount_IsCreatedLinkedAndConfigured_InOneStep()
    {
        var admin = await this.SignInAsync();
        var employeeId = await this.CreateEmployeeAsync(admin);

        var created = await admin.PostAsJsonAsync("/api/auth/register", new
        {
            email = NewEmail(),
            password = "Direktor123!",
            roleCodes = new[] { "EMPLOYEE", "EVALUATOR" },
            employeeId,
            controllerEmployeeId = (long?)null,
        });

        created.StatusCode.Should().Be(HttpStatusCode.OK, await created.Content.ReadAsStringAsync());
        (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("employeeId").GetInt64().Should().Be(employeeId);
        var settings = await admin.GetFromJsonAsync<JsonElement>($"/api/evaluator-settings/{employeeId}");
        settings.GetProperty("controllerEmployeeId").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task AnEmployeeAccount_WithoutAnEmployee_IsRefused()
    {
        var admin = await this.SignInAsync();

        var created = await admin.PostAsJsonAsync("/api/auth/register", new
        {
            email = NewEmail(),
            password = "Zaposleni123!",
            roleCodes = new[] { "EMPLOYEE" },
        });

        await ShouldFailAsync(created, ErrorCodes.EmployeeRequiredForRoles);
    }

    [Fact]
    public async Task ASecondAccount_ForTheSameEmployee_IsRefused()
    {
        var admin = await this.SignInAsync();
        var employeeId = await this.CreateEmployeeAsync(admin);
        (await this.RegisterAsync(admin, ["EMPLOYEE"], employeeId)).StatusCode.Should().Be(HttpStatusCode.OK);

        await ShouldFailAsync(await this.RegisterAsync(admin, ["EMPLOYEE"], employeeId), ErrorCodes.EmployeeAlreadyHasAccount);
    }

    [Fact]
    public async Task TheLinkedAccount_CannotBeSwappedForAnother()
    {
        var admin = await this.SignInAsync();
        var employeeId = await this.CreateEmployeeAsync(admin);
        await this.RegisterAsync(admin, ["PAYROLL"], employeeId);
        var otherUserId = await this.UserIdAsync(await this.RegisterAsync(admin, ["PAYROLL"], employeeId: null));

        var swapped = await admin.PutAsJsonAsync($"/api/employees/{employeeId}/user", new
        {
            userId = otherUserId,
            version = await this.EmployeeVersionAsync(admin, employeeId),
        });

        await ShouldFailAsync(swapped, ErrorCodes.EmployeeAccountChangeRequiresUnlink);
    }

    [Fact]
    public async Task AnAccountWithAnEmployeeRole_CannotBeUnlinked()
    {
        var admin = await this.SignInAsync();
        var employeeId = await this.CreateEmployeeAsync(admin);
        await this.RegisterAsync(admin, ["EMPLOYEE"], employeeId);

        var unlinked = await admin.PutAsJsonAsync($"/api/employees/{employeeId}/user", new
        {
            userId = (long?)null,
            version = await this.EmployeeVersionAsync(admin, employeeId),
        });

        await ShouldFailAsync(unlinked, ErrorCodes.AccountRolesRequireEmployee);
    }

    [Fact]
    public async Task AnAccountWithoutEmployeeRoles_CanBeUnlinked()
    {
        var admin = await this.SignInAsync();
        var employeeId = await this.CreateEmployeeAsync(admin);
        await this.RegisterAsync(admin, ["PAYROLL"], employeeId);

        var unlinked = await admin.PutAsJsonAsync($"/api/employees/{employeeId}/user", new
        {
            userId = (long?)null,
            version = await this.EmployeeVersionAsync(admin, employeeId),
        });

        unlinked.StatusCode.Should().Be(HttpStatusCode.OK, await unlinked.Content.ReadAsStringAsync());
        (await unlinked.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").ValueKind.Should().Be(JsonValueKind.Null);
    }

    private Task<HttpResponseMessage> RegisterAsync(HttpClient admin, string[] roleCodes, long? employeeId) =>
        admin.PostAsJsonAsync("/api/auth/register", new
        {
            email = NewEmail(),
            password = "Nalog1234!",
            roleCodes,
            employeeId,
        });

    private async Task<long> UserIdAsync(HttpResponseMessage registered)
    {
        registered.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await registered.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt64();
    }

    private async Task<int> EmployeeVersionAsync(HttpClient admin, long employeeId) =>
        (await admin.GetFromJsonAsync<JsonElement>($"/api/employees/{employeeId}")).GetProperty("version").GetInt32();

    private async Task<long> CreateEmployeeAsync(HttpClient admin)
    {
        var orgUnits = await admin.GetFromJsonAsync<JsonElement>("/api/organization-units");
        var positions = await admin.GetFromJsonAsync<JsonElement>("/api/job-positions");
        var educations = await admin.GetFromJsonAsync<JsonElement>("/api/education-levels");
        var created = await admin.PostAsJsonAsync("/api/employees", new
        {
            firstName = "Nalog",
            lastName = Guid.NewGuid().ToString("N")[..8],
            organizationUnitId = orgUnits[0].GetProperty("id").GetInt64(),
            jobPositionId = positions[0].GetProperty("id").GetInt64(),
            educationLevelId = educations[0].GetProperty("id").GetInt64(),
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt64();
    }

    private static async Task ShouldFailAsync(HttpResponseMessage response, string errorCode)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString().Should().Be(errorCode);
    }

    private static string NewEmail() => $"nalog-{Guid.NewGuid():N}@local.dev";

    private async Task<HttpClient> SignInAsync()
    {
        var client = this.fixture.Factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestCredentials.AdminEmail, password = TestCredentials.AdminPassword });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

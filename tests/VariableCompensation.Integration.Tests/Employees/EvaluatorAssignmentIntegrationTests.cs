using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using VariableCompensation.Domain;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Employees;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class EvaluatorAssignmentIntegrationTests
{
    private readonly IntegrationTestFixture fixture;

    public EvaluatorAssignmentIntegrationTests(IntegrationTestFixture fixture)
    {
        this.fixture = fixture;
    }

    private sealed record EmployeePayload(
        string FirstName,
        string LastName,
        long OrganizationUnitId,
        long JobPositionId,
        long EducationLevelId,
        long? EvaluatorEmployeeId,
        bool IsActive = true);

    // The seeded evaluator has evaluator_settings; the seeded employee does not.
    [Fact]
    public async Task Create_WithAConfiguredEvaluator_Succeeds()
    {
        var client = await this.CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/employees",
            await this.NewEmployeeAsync(client, TestEmployeeIds.Evaluator));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_WithSomeoneWhoIsNotAnEvaluator_IsRejected()
    {
        var client = await this.CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/employees",
            await this.NewEmployeeAsync(client, TestEmployeeIds.Employee));

        await ShouldFailWithEvaluatorNotConfiguredAsync(response);
    }

    [Fact]
    public async Task Update_WithSomeoneWhoIsNotAnEvaluator_IsRejected()
    {
        var client = await this.CreateAdminClientAsync();

        var created = await client.PostAsJsonAsync("/api/employees", await this.NewEmployeeAsync(client, null));
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt64();

        var response = await client.PutAsJsonAsync(
            $"/api/employees/{id}",
            await this.NewEmployeeAsync(client, TestEmployeeIds.Employee));

        await ShouldFailWithEvaluatorNotConfiguredAsync(response);
    }

    private static async Task ShouldFailWithEvaluatorNotConfiguredAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("error").GetString().Should().Be(ErrorCodes.EvaluatorNotConfigured);
    }

    private async Task<EmployeePayload> NewEmployeeAsync(HttpClient client, long? evaluatorEmployeeId)
    {
        var orgUnits = await client.GetFromJsonAsync<JsonElement>("/api/organization-units");
        var positions = await client.GetFromJsonAsync<JsonElement>("/api/job-positions");
        var educations = await client.GetFromJsonAsync<JsonElement>("/api/education-levels");

        return new EmployeePayload(
            "Novi",
            $"Zaposleni {Guid.NewGuid():N}",
            orgUnits[0].GetProperty("id").GetInt64(),
            positions[0].GetProperty("id").GetInt64(),
            educations[0].GetProperty("id").GetInt64(),
            evaluatorEmployeeId);
    }

    private async Task<HttpClient> CreateAdminClientAsync()
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

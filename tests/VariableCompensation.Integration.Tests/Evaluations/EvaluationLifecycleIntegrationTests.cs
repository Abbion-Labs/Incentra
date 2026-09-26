using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Evaluations;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class EvaluationLifecycleIntegrationTests
{
    private readonly IntegrationTestFixture fixture;

    public EvaluationLifecycleIntegrationTests(IntegrationTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task FullWorkflow_CreateSubmitApprove_Succeeds()
    {
        var evaluatorClient = await this.CreateClientAsync(TestCredentials.EvaluatorEmail, TestCredentials.EvaluatorPassword);
        var controllerClient = await this.CreateClientAsync(TestCredentials.ControllerEmail, TestCredentials.ControllerPassword);

        var createResponse = await evaluatorClient.PostAsJsonAsync("/api/evaluations", new
        {
            employeeId = TestEmployeeIds.Employee,
            year = 2026,
            quarter = 2,
        });
        createResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var evaluationId = created.GetProperty("id").GetInt64();
        var version = created.GetProperty("version").GetInt32();

        var ratingLevels = await evaluatorClient.GetFromJsonAsync<JsonElement>("/api/lookups/rating-levels");
        var ratedLevelId = ratingLevels.EnumerateArray().First(e => e.GetProperty("value").GetInt32() == 3).GetProperty("id").GetInt64();
        var measureTypes = await evaluatorClient.GetFromJsonAsync<JsonElement>("/api/lookups/measure-types");
        var measureTypeId = measureTypes.EnumerateArray().First().GetProperty("id").GetInt64();

        var goalsResponse = await evaluatorClient.PutAsJsonAsync($"/api/evaluations/{evaluationId}/goals", new
        {
            version,
            goals = new[]
            {
                new { description = "Povećati produktivnost", ratingLevelId = ratedLevelId, weight = 100m, sortOrder = 1 },
            },
        });
        goalsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        version = (await goalsResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetInt32();
        version = await CompletePlanAsync(evaluatorClient, evaluationId, version);

        var measuresResponse = await evaluatorClient.PutAsJsonAsync($"/api/evaluations/{evaluationId}/measures", new
        {
            version,
            measures = new[]
            {
                new { measureTypeId, ratingLevelId = ratedLevelId, sortOrder = 1 },
            },
        });
        measuresResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        version = (await measuresResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetInt32();

        var submitResponse = await evaluatorClient.PostAsJsonAsync($"/api/evaluations/{evaluationId}/submit", new { version });
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        version = (await submitResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetInt32();

        var reviewResponse = await controllerClient.PostAsJsonAsync($"/api/evaluations/{evaluationId}/start-review", new { version });
        reviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        version = (await reviewResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetInt32();

        var approveResponse = await controllerClient.PostAsJsonAsync($"/api/evaluations/{evaluationId}/approve", new
        {
            version,
            controllerComment = "Odobreno",
        });
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var approved = await approveResponse.Content.ReadFromJsonAsync<JsonElement>();
        approved.GetProperty("status").GetString().Should().Be("Approved");

        var historyResponse = await evaluatorClient.GetAsync($"/api/evaluations/{evaluationId}/status-history");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await historyResponse.Content.ReadFromJsonAsync<JsonElement>();
        history.GetArrayLength().Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task Submit_WithoutMeasures_ReturnsBadRequest()
    {
        var evaluatorClient = await this.CreateClientAsync(TestCredentials.EvaluatorEmail, TestCredentials.EvaluatorPassword);

        var createResponse = await evaluatorClient.PostAsJsonAsync("/api/evaluations", new
        {
            employeeId = TestEmployeeIds.Employee,
            year = 2026,
            quarter = 3,
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var evaluationId = created.GetProperty("id").GetInt64();
        var version = created.GetProperty("version").GetInt32();

        var ratingLevels = await evaluatorClient.GetFromJsonAsync<JsonElement>("/api/lookups/rating-levels");
        var ratedLevelId = ratingLevels.EnumerateArray().First(e => e.GetProperty("value").GetInt32() == 3).GetProperty("id").GetInt64();

        await evaluatorClient.PutAsJsonAsync($"/api/evaluations/{evaluationId}/goals", new
        {
            version,
            goals = new[] { new { description = "Cilj", ratingLevelId = ratedLevelId, weight = 100m, sortOrder = 1 } },
        });
        version = await CompletePlanAsync(evaluatorClient, evaluationId, version + 1);

        var submitResponse = await evaluatorClient.PostAsJsonAsync($"/api/evaluations/{evaluationId}/submit", new { version });
        submitResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await submitResponse.Content.ReadAsStringAsync();
        body.Should().Contain("vn-0026");
    }

    /// <summary>Adds the conditions and criteria that complete the plan, and returns the version after them.</summary>
    private static async Task<int> CompletePlanAsync(HttpClient client, long evaluationId, int version)
    {
        var conditions = await client.PutAsJsonAsync($"/api/evaluations/{evaluationId}/conditions", new
        {
            version,
            conditions = new[] { new { description = "Uslov", sortOrder = 1 } },
        });
        conditions.StatusCode.Should().Be(HttpStatusCode.OK, await conditions.Content.ReadAsStringAsync());
        version = (await conditions.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetInt32();

        var criteria = await client.PutAsJsonAsync($"/api/evaluations/{evaluationId}/criteria", new
        {
            version,
            criteria = new[] { new { description = "Kriterijum", sortOrder = 1 } },
        });
        criteria.StatusCode.Should().Be(HttpStatusCode.OK, await criteria.Content.ReadAsStringAsync());
        return (await criteria.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetInt32();
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

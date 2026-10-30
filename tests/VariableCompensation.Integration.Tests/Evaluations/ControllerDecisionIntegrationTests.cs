using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Evaluations;

/// <summary>
/// A controller decides on a submitted evaluation in one request. The evaluation is taken into review on the way,
/// so its history reads the same as when the review was opened first. The tab counts come from one query and
/// still agree with the lists behind the tabs. The tests use 2021 and 2022, which no other test uses and which stay
/// inside the years an evaluation can be created for.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class ControllerDecisionIntegrationTests
{
    private readonly IntegrationTestFixture fixture;

    public ControllerDecisionIntegrationTests(IntegrationTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Approve_StraightFromSubmitted_RecordsTheReviewStep()
    {
        var evaluator = await this.CreateClientAsync(TestCredentials.EvaluatorEmail, TestCredentials.EvaluatorPassword);
        var controller = await this.CreateClientAsync(TestCredentials.ControllerEmail, TestCredentials.ControllerPassword);
        var (id, version) = await SubmitEvaluationAsync(evaluator, year: 2021, quarter: 1);

        var response = await controller.PostAsJsonAsync($"/api/evaluations/{id}/approve", new
        {
            version,
            controllerComment = "U redu",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var approved = await response.Content.ReadFromJsonAsync<JsonElement>();
        approved.GetProperty("status").GetString().Should().Be("Approved");
        (await LatestStepsAsync(controller, id, 2)).Should().Equal("UnderReview>Approved", "Submitted>UnderReview");
    }

    [Fact]
    public async Task ReturnForRevision_StraightFromSubmitted_RecordsTheReviewStep()
    {
        var evaluator = await this.CreateClientAsync(TestCredentials.EvaluatorEmail, TestCredentials.EvaluatorPassword);
        var controller = await this.CreateClientAsync(TestCredentials.ControllerEmail, TestCredentials.ControllerPassword);
        var (id, version) = await SubmitEvaluationAsync(evaluator, year: 2021, quarter: 2);

        var response = await controller.PostAsJsonAsync($"/api/evaluations/{id}/return-for-revision", new
        {
            version,
            revisionComment = "Dopuniti ciljeve",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var returned = await response.Content.ReadFromJsonAsync<JsonElement>();
        returned.GetProperty("status").GetString().Should().Be("Draft");
        (await LatestStepsAsync(controller, id, 2)).Should().Equal("UnderReview>Draft", "Submitted>UnderReview");
    }

    [Fact]
    public async Task BucketCounts_MatchTheListBehindEachTab()
    {
        var evaluator = await this.CreateClientAsync(TestCredentials.EvaluatorEmail, TestCredentials.EvaluatorPassword);
        // An evaluation still being planned, so at least one tab is not empty.
        var create = await evaluator.PostAsJsonAsync("/api/evaluations", new
        {
            employeeId = TestEmployeeIds.Employee,
            year = 2022,
            quarter = 1,
        });
        create.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);

        var counts = await evaluator.GetFromJsonAsync<JsonElement>("/api/evaluator/evaluations/bucket-counts");

        var tabs = new Dictionary<string, string>
        {
            ["planning"] = "planning",
            ["unrated"] = "unrated",
            ["returned"] = "returned",
            ["submitted"] = "submitted",
            ["approved"] = "approved",
            ["goalsComplete"] = "goalscomplete",
        };
        foreach (var (countName, bucket) in tabs)
        {
            var list = await evaluator.GetFromJsonAsync<JsonElement>($"/api/evaluator/evaluations?bucket={bucket}&pageSize=1");
            counts.GetProperty(countName).GetInt32().Should().Be(list.GetProperty("totalCount").GetInt32(), bucket);
        }

        counts.GetProperty("planning").GetInt32().Should().BeGreaterThan(0);
        counts.GetProperty("pending").GetInt32().Should().Be(counts.GetProperty("submitted").GetInt32());
    }

    /// <summary>Creates an evaluation with a complete plan and ratings and submits it; returns its id and version.</summary>
    private static async Task<(long Id, int Version)> SubmitEvaluationAsync(HttpClient evaluator, short year, byte quarter)
    {
        var create = await evaluator.PostAsJsonAsync("/api/evaluations", new
        {
            employeeId = TestEmployeeIds.Employee,
            year,
            quarter,
        });
        create.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetInt64();
        var version = created.GetProperty("version").GetInt32();

        var ratingLevels = await evaluator.GetFromJsonAsync<JsonElement>("/api/lookups/rating-levels");
        var ratedLevelId = ratingLevels.EnumerateArray()
            .First(level => level.GetProperty("value").GetInt32() == 3)
            .GetProperty("id").GetInt64();
        var measureTypes = await evaluator.GetFromJsonAsync<JsonElement>("/api/lookups/measure-types");
        var measureTypeId = measureTypes.EnumerateArray().First().GetProperty("id").GetInt64();

        version = await PutAsync(evaluator, $"/api/evaluations/{id}/goals", new
        {
            version,
            goals = new[] { new { description = "Cilj", ratingLevelId = ratedLevelId, weight = 100m, sortOrder = 1 } },
        });
        version = await PutAsync(evaluator, $"/api/evaluations/{id}/conditions", new
        {
            version,
            conditions = new[] { new { description = "Uslov", sortOrder = 1 } },
        });
        version = await PutAsync(evaluator, $"/api/evaluations/{id}/criteria", new
        {
            version,
            criteria = new[] { new { description = "Kriterijum", sortOrder = 1 } },
        });
        version = await PutAsync(evaluator, $"/api/evaluations/{id}/measures", new
        {
            version,
            measures = new[] { new { measureTypeId, ratingLevelId = ratedLevelId, sortOrder = 1 } },
        });

        var submit = await evaluator.PostAsJsonAsync($"/api/evaluations/{id}/submit", new { version });
        submit.StatusCode.Should().Be(HttpStatusCode.OK, await submit.Content.ReadAsStringAsync());
        var submitted = await submit.Content.ReadFromJsonAsync<JsonElement>();
        submitted.GetProperty("status").GetString().Should().Be("Submitted");
        return (id, submitted.GetProperty("version").GetInt32());
    }

    private static async Task<int> PutAsync(HttpClient client, string path, object body)
    {
        var response = await client.PutAsJsonAsync(path, body);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetInt32();
    }

    /// <summary>The newest status changes, newest first, as "From>To".</summary>
    private static async Task<IReadOnlyList<string>> LatestStepsAsync(HttpClient client, long id, int count)
    {
        var history = await client.GetFromJsonAsync<JsonElement>($"/api/evaluations/{id}/status-history");
        return history.EnumerateArray()
            .Take(count)
            .Select(step => $"{step.GetProperty("fromStatus").GetString()}>{step.GetProperty("toStatus").GetString()}")
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

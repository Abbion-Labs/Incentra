using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Evaluation;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Domain.Enums;
using VariableCompensation.Infrastructure.Persistence;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Evaluations;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class AgreedGoalsIntegrationTests
{
    private readonly IntegrationTestFixture fixture;

    public AgreedGoalsIntegrationTests(IntegrationTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task OnceThePlanIsSet_RatingKeepsTheAgreedGoals()
    {
        var (evaluationId, goalId, ratedLevelId) = await this.SeedAsync();
        var evaluator = await this.CreateClientAsync(TestCredentials.EvaluatorEmail, TestCredentials.EvaluatorPassword);

        object Body(int version, object[] goals) => new
        {
            version,
            conversationAt = (DateTime?)null,
            evaluatorComment = (string?)null,
            conditionsNotMetComment = (string?)null,
            conditionsFulfilled = true,
            goals,
        };

        // Wiping the goals is refused.
        var wiped = await evaluator.PutAsJsonAsync($"/api/evaluations/{evaluationId}/rating-draft", Body(1, []));
        wiped.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await wiped.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()
            .Should().Be(ErrorCodes.GoalsPlanningLocked);

        // Rating the agreed goal updates that very goal.
        var rated = await evaluator.PutAsJsonAsync($"/api/evaluations/{evaluationId}/rating-draft", Body(1,
        [
            new { id = goalId, description = "Dogovoreni cilj", ratingLevelId = ratedLevelId, comment = "Ostvaren", weight = (decimal?)null, sortOrder = 1 },
        ]));
        rated.StatusCode.Should().Be(HttpStatusCode.OK, await rated.Content.ReadAsStringAsync());

        using var scope = this.fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var goal = await context.EvaluationGoals.AsNoTracking().SingleAsync(g => g.EvaluationId == evaluationId);
        goal.Id.Should().Be(goalId);
        goal.Description.Should().Be("Dogovoreni cilj");
        goal.RatingLevelId.Should().Be(ratedLevelId);
        goal.Comment.Should().Be("Ostvaren");
    }

    private async Task<(long EvaluationId, long GoalId, long RatedLevelId)> SeedAsync()
    {
        using var scope = this.fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notRated = await context.RatingLevels.SingleAsync(r => r.Value == 0);
        var rated = await context.RatingLevels.SingleAsync(r => r.Value == 4);

        var employee = new Employee
        {
            FirstName = "Plan",
            LastName = Guid.NewGuid().ToString("N")[..8],
            OrganizationUnitId = (await context.OrganizationUnits.FirstAsync()).Id,
            JobPositionId = (await context.JobPositions.FirstAsync()).Id,
            EducationLevelId = (await context.EducationLevels.FirstAsync()).Id,
            EvaluatorEmployeeId = TestEmployeeIds.Evaluator,
            IsActive = true,
        };
        var evaluation = new Evaluation
        {
            Employee = employee,
            EvaluatorEmployeeId = TestEmployeeIds.Evaluator,
            ControllerEmployeeId = TestEmployeeIds.Controller,
            Year = 2024,
            Quarter = 3,
            Status = EvaluationStatus.Draft,
            ConditionsFulfilled = true,
            Version = 1,
        };
        evaluation.Goals.Add(new EvaluationGoal { Description = "Dogovoreni cilj", RatingLevelId = notRated.Id, SortOrder = 1 });
        evaluation.Conditions.Add(new EvaluationCondition { Description = "Uslov", SortOrder = 1 });
        evaluation.Criteria.Add(new EvaluationCriterion { Description = "Kriterijum", SortOrder = 1 });
        context.Evaluations.Add(evaluation);
        await context.SaveChangesAsync();

        return (evaluation.Id, evaluation.Goals.Single().Id, rated.Id);
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

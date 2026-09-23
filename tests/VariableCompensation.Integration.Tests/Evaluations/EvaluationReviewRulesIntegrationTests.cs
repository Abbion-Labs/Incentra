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
using VariableCompensation.Domain.Entities.Identity;
using VariableCompensation.Domain.Enums;
using VariableCompensation.Infrastructure.Persistence;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Evaluations;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class EvaluationReviewRulesIntegrationTests
{
    private const string EvaluatorPassword = "Direktor123!";

    private readonly IntegrationTestFixture fixture;

    public EvaluationReviewRulesIntegrationTests(IntegrationTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task EvaluatorWithoutController_SubmissionApprovesTheEvaluation()
    {
        var seed = await this.SeedAsync();
        var admin = await this.CreateClientAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);

        // Their own controller is not allowed; no controller is.
        var ownController = await admin.PutAsJsonAsync(
            $"/api/evaluator-settings/{seed.EvaluatorId}",
            new { controllerEmployeeId = seed.EvaluatorId });
        ownController.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ownController.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()
            .Should().Be(ErrorCodes.EvaluatorOwnController);

        var noController = await admin.PutAsJsonAsync(
            $"/api/evaluator-settings/{seed.EvaluatorId}",
            new { controllerEmployeeId = (long?)null });
        noController.StatusCode.Should().Be(HttpStatusCode.OK);

        // An administrator takes no part in rating.
        var adminSubmit = await admin.PostAsJsonAsync($"/api/evaluations/{seed.EvaluationId}/submit", new { version = 1 });
        adminSubmit.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var evaluator = await this.CreateClientAsync(seed.EvaluatorEmail, EvaluatorPassword);
        var submit = await evaluator.PostAsJsonAsync($"/api/evaluations/{seed.EvaluationId}/submit", new { version = 2 });
        submit.StatusCode.Should().Be(HttpStatusCode.OK);
        (await submit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString()
            .Should().Be(nameof(EvaluationStatus.Approved));
    }

    [Fact]
    public async Task Administrator_CannotReviewEvaluations()
    {
        var admin = await this.CreateClientAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);

        var approve = await admin.PostAsJsonAsync("/api/evaluations/1/approve", new { version = 1 });
        var start = await admin.PostAsJsonAsync("/api/evaluations/1/start-review", new { version = 1 });
        var returned = await admin.PostAsJsonAsync(
            "/api/evaluations/1/return-for-revision",
            new { version = 1, revisionComment = "x" });

        approve.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        start.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        returned.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<Seed> SeedAsync()
    {
        using var scope = this.fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var orgUnit = await context.OrganizationUnits.FirstAsync();
        var jobPosition = await context.JobPositions.FirstAsync();
        var education = await context.EducationLevels.FirstAsync();
        var evaluatorRole = await context.Roles.SingleAsync(r => r.Code == RoleCodes.Evaluator);
        var rated = await context.RatingLevels.FirstAsync(r => r.Value == 4);
        var measureType = await context.MeasureTypes.FirstAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"direktor-{suffix}@local.dev";

        Employee NewEmployee(string firstName, long? evaluatorId) => new()
        {
            FirstName = firstName,
            LastName = suffix,
            OrganizationUnitId = orgUnit.Id,
            JobPositionId = jobPosition.Id,
            EducationLevelId = education.Id,
            EvaluatorEmployeeId = evaluatorId,
            IsActive = true,
        };

        var evaluator = NewEmployee("Direktor", null);
        evaluator.User = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(EvaluatorPassword, workFactor: 4),
            UserRoles = { new UserRole { RoleId = evaluatorRole.Id } },
        };
        context.Employees.Add(evaluator);
        await context.SaveChangesAsync();

        context.EvaluatorSettings.Add(new EvaluatorSettings
        {
            EmployeeId = evaluator.Id,
            ControllerEmployeeId = TestEmployeeIds.Controller,
        });
        var subordinate = NewEmployee("Saradnik", evaluator.Id);
        context.Employees.Add(subordinate);
        await context.SaveChangesAsync();

        var evaluation = new Evaluation
        {
            EmployeeId = subordinate.Id,
            EvaluatorEmployeeId = evaluator.Id,
            ControllerEmployeeId = TestEmployeeIds.Controller,
            Year = 2024,
            Quarter = 4,
            Status = EvaluationStatus.Draft,
            ConditionsFulfilled = true,
            Version = 1,
        };
        evaluation.Goals.Add(new EvaluationGoal { Description = "Cilj", RatingLevelId = rated.Id, SortOrder = 1 });
        evaluation.Measures.Add(new EvaluationMeasure { MeasureTypeId = measureType.Id, RatingLevelId = rated.Id, SortOrder = 1 });
        context.Evaluations.Add(evaluation);
        await context.SaveChangesAsync();

        return new Seed(evaluator.Id, email, evaluation.Id);
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

    private sealed record Seed(long EvaluatorId, string EvaluatorEmail, long EvaluationId);
}

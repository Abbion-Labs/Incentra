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
public class EvaluationReassignmentIntegrationTests
{
    private const short Year = 2025;

    private readonly IntegrationTestFixture fixture;

    public EvaluationReassignmentIntegrationTests(IntegrationTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task ChangingAssignments_MovesOpenEvaluationsAndKeepsSubmittedOnesUntilReturned()
    {
        var seed = await this.SeedAsync();
        var admin = await this.CreateClientAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);

        // The employee gets a new evaluator: the draft moves, the submitted and approved evaluations stay.
        var reassign = await admin.PutAsJsonAsync($"/api/employees/{seed.EmployeeId}", EmployeeBody(seed, seed.NewEvaluatorId, version: 0));
        reassign.StatusCode.Should().Be(HttpStatusCode.OK);

        (await this.AssignmentAsync(seed.DraftId)).Should().Be((seed.NewEvaluatorId, seed.NewControllerId));
        (await this.AssignmentAsync(seed.SubmittedId)).Should().Be((TestEmployeeIds.Evaluator, TestEmployeeIds.Controller));
        (await this.AssignmentAsync(seed.ApprovedId)).Should().Be((TestEmployeeIds.Evaluator, TestEmployeeIds.Controller));

        // Evaluations that are not approved yet need an evaluator.
        var removal = await admin.PutAsJsonAsync($"/api/employees/{seed.EmployeeId}", EmployeeBody(seed, null, version: 1));
        removal.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await removal.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()
            .Should().Be(ErrorCodes.EmployeeHasOpenEvaluations);

        // Returned for revision, the submitted one goes to the evaluator who rates the employee now.
        var controller = await this.CreateClientAsync(TestCredentials.ControllerEmail, TestCredentials.ControllerPassword);
        var returned = await controller.PostAsJsonAsync($"/api/evaluations/{seed.SubmittedId}/return-for-revision", new
        {
            version = 1,
            revisionComment = "Dopuniti merila",
        });
        returned.StatusCode.Should().Be(HttpStatusCode.OK);
        (await this.AssignmentAsync(seed.SubmittedId)).Should().Be((seed.NewEvaluatorId, seed.NewControllerId));

        // The new evaluator gets another controller: their open evaluations follow, the approved one stays.
        var settings = await admin.PutAsJsonAsync(
            $"/api/evaluator-settings/{seed.NewEvaluatorId}",
            new { controllerEmployeeId = seed.OtherControllerId, version = 0 });
        settings.StatusCode.Should().Be(HttpStatusCode.OK);

        (await this.AssignmentAsync(seed.DraftId)).Should().Be((seed.NewEvaluatorId, seed.OtherControllerId));
        (await this.AssignmentAsync(seed.SubmittedId)).Should().Be((seed.NewEvaluatorId, seed.OtherControllerId));
        (await this.AssignmentAsync(seed.ApprovedId)).Should().Be((TestEmployeeIds.Evaluator, TestEmployeeIds.Controller));
    }

    private static object EmployeeBody(Seed seed, long? evaluatorEmployeeId, int version) => new
    {
        version,
        firstName = "Premeštanje",
        lastName = "Ocena",
        organizationUnitId = seed.OrganizationUnitId,
        jobPositionId = seed.JobPositionId,
        educationLevelId = seed.EducationLevelId,
        evaluatorEmployeeId,
        hiredAt = (string?)null,
        isActive = true,
    };

    private async Task<(long EvaluatorId, long? ControllerId)> AssignmentAsync(long evaluationId)
    {
        using var scope = this.fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var evaluation = await context.Evaluations.AsNoTracking().SingleAsync(e => e.Id == evaluationId);
        return (evaluation.EvaluatorEmployeeId, evaluation.ControllerEmployeeId);
    }

    private async Task<Seed> SeedAsync()
    {
        using var scope = this.fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var orgUnit = await context.OrganizationUnits.FirstAsync();
        var jobPosition = await context.JobPositions.FirstAsync();
        var education = await context.EducationLevels.FirstAsync();
        var controllerRole = await context.Roles.SingleAsync(r => r.Code == RoleCodes.Controller);
        var evaluatorRole = await context.Roles.SingleAsync(r => r.Code == RoleCodes.Evaluator);
        var suffix = Guid.NewGuid().ToString("N")[..8];

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

        Employee NewController(string firstName)
        {
            var controller = NewEmployee(firstName, null);
            controller.User = new User
            {
                Email = $"{firstName.ToLowerInvariant()}-{suffix}@local.dev",
                PasswordHash = "unused",
                UserRoles = { new UserRole { RoleId = controllerRole.Id } },
            };
            return controller;
        }

        // Two more controllers, and a second evaluator who reports to the first of them.
        var newController = NewController("Petar");
        var otherController = NewController("Nikola");
        var newEvaluator = NewEmployee("Ana", null);
        newEvaluator.User = new User
        {
            Email = $"ana-{suffix}@local.dev",
            PasswordHash = "unused",
            UserRoles = { new UserRole { RoleId = evaluatorRole.Id } },
        };
        var employee = NewEmployee("Premeštanje", TestEmployeeIds.Evaluator);
        context.Employees.AddRange(newController, otherController, newEvaluator, employee);
        await context.SaveChangesAsync();

        context.EvaluatorSettings.Add(new EvaluatorSettings
        {
            EmployeeId = newEvaluator.Id,
            ControllerEmployeeId = newController.Id,
        });

        Evaluation NewEvaluation(byte quarter, EvaluationStatus status)
        {
            var evaluation = new Evaluation
            {
                EmployeeId = employee.Id,
                EvaluatorEmployeeId = TestEmployeeIds.Evaluator,
                ControllerEmployeeId = TestEmployeeIds.Controller,
                Year = Year,
                Quarter = quarter,
                Status = status,
                Version = 1,
            };
            context.Evaluations.Add(evaluation);
            return evaluation;
        }

        var draft = NewEvaluation(1, EvaluationStatus.Draft);
        var submitted = NewEvaluation(2, EvaluationStatus.Submitted);
        var approved = NewEvaluation(3, EvaluationStatus.Approved);
        await context.SaveChangesAsync();

        return new Seed(
            employee.Id,
            orgUnit.Id,
            jobPosition.Id,
            education.Id,
            newEvaluator.Id,
            newController.Id,
            otherController.Id,
            draft.Id,
            submitted.Id,
            approved.Id);
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

    private sealed record Seed(
        long EmployeeId,
        long OrganizationUnitId,
        long JobPositionId,
        long EducationLevelId,
        long NewEvaluatorId,
        long NewControllerId,
        long OtherControllerId,
        long DraftId,
        long SubmittedId,
        long ApprovedId);
}

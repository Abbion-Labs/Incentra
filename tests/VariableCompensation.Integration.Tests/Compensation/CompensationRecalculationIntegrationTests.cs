using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VariableCompensation.Application.Abstractions.Security;
using VariableCompensation.Domain.Entities.Evaluation;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Domain.Entities.Lookup;
using VariableCompensation.Domain.Enums;
using VariableCompensation.Infrastructure.Persistence;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Compensation;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class CompensationRecalculationIntegrationTests
{
    private const short Year = 2026;
    private const decimal MonetaryPool = 100_000m;

    private readonly IntegrationTestFixture fixture;

    public CompensationRecalculationIntegrationTests(IntegrationTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Recalculate_DropsResultsOfEmployeesLeftOut()
    {
        var (organizationUnitId, keptEmployeeId, droppedEmployeeId) = await this.SeedUnitAsync();
        var payroll = await this.CreateClientAsync(TestCredentials.PayrollEmail, TestCredentials.PayrollPassword);

        var createResponse = await payroll.PostAsJsonAsync("/api/compensation-parameters", new
        {
            organizationUnitId,
            year = Year,
            monetaryPool = MonetaryPool,
            currency = "RSD",
            acceptablePerformanceRating = 3.0m,
            upperLimitCoefficient = 0.25m,
            dependencyWeight = 0.5m,
            exponent = 1.0m,
            allowNegativeVariable = false,
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var parametersId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt64();

        var first = await payroll.PostAsJsonAsync($"/api/compensation-parameters/{parametersId}/calculate", new { isFinal = false });
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        (await this.ResultsAsync(parametersId)).Select(r => r.EmployeeId)
            .Should().BeEquivalentTo([keptEmployeeId, droppedEmployeeId]);

        // Without a salary the employee is skipped on the next calculation.
        await this.WithContextAsync(context =>
            context.EmployeeSalaries.Where(s => s.EmployeeId == droppedEmployeeId).ExecuteDeleteAsync());

        var second = await payroll.PostAsJsonAsync($"/api/compensation-parameters/{parametersId}/calculate", new { isFinal = false });
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var results = await this.ResultsAsync(parametersId);
        results.Select(r => r.EmployeeId).Should().Equal(keptEmployeeId);
        results.Sum(r => r.NetCompensation).Should().Be(MonetaryPool);
    }

    private async Task<List<(long EmployeeId, decimal NetCompensation)>> ResultsAsync(long parametersId)
    {
        List<(long, decimal)> results = [];
        await this.WithContextAsync(async context =>
        {
            var rows = await context.VariableCompensationResults
                .Where(r => r.ParametersId == parametersId)
                .Select(r => new { r.EmployeeId, r.NetCompensation })
                .ToListAsync();
            results = rows.Select(r => (r.EmployeeId, r.NetCompensation)).ToList();
        });
        return results;
    }

    private async Task<(long OrganizationUnitId, long KeptEmployeeId, long DroppedEmployeeId)> SeedUnitAsync()
    {
        (long, long, long) ids = default;
        await this.WithContextAsync(async context =>
        {
            var encryption = this.fixture.Factory.Services.GetRequiredService<ISensitiveDataEncryptionService>();
            var jobPosition = await context.JobPositions.FirstAsync();
            var education = await context.EducationLevels.FirstAsync();
            var rated = await context.RatingLevels.FirstAsync(r => r.Value == 4);
            var measureType = await context.MeasureTypes.FirstAsync();

            var unit = new OrganizationUnit { Name = $"Recalculation {Guid.NewGuid():N}", IsActive = true };
            context.OrganizationUnits.Add(unit);
            await context.SaveChangesAsync();

            Employee AddEmployee(string lastName, int points)
            {
                var employee = new Employee
                {
                    FirstName = "Obračun",
                    LastName = lastName,
                    OrganizationUnitId = unit.Id,
                    JobPositionId = jobPosition.Id,
                    EducationLevelId = education.Id,
                    EvaluatorEmployeeId = TestEmployeeIds.Evaluator,
                    IsActive = true,
                };
                context.Employees.Add(employee);
                context.EmployeeSalaries.Add(new EmployeeSalary
                {
                    Employee = employee,
                    Points = points,
                    EncryptedSalaryPerPoint = encryption.EncryptDecimal(100m),
                    EffectiveFrom = new DateOnly(Year, 1, 1),
                });

                var evaluation = new Evaluation
                {
                    Employee = employee,
                    EvaluatorEmployeeId = TestEmployeeIds.Evaluator,
                    ControllerEmployeeId = TestEmployeeIds.Controller,
                    Year = Year,
                    Quarter = 1,
                    Status = EvaluationStatus.Approved,
                    ConditionsFulfilled = true,
                    GoalsAverage = 4.0m,
                    MeasuresAverage = 4.0m,
                    OverallAverage = 4.0m,
                    ApprovedAt = DateTime.UtcNow,
                };
                evaluation.Goals.Add(new EvaluationGoal { Description = "Cilj", RatingLevelId = rated.Id, SortOrder = 1 });
                evaluation.Measures.Add(new EvaluationMeasure { MeasureTypeId = measureType.Id, RatingLevelId = rated.Id, SortOrder = 1 });
                context.Evaluations.Add(evaluation);
                return employee;
            }

            var kept = AddEmployee("Ostaje", 400);
            var dropped = AddEmployee("Ispada", 200);
            await context.SaveChangesAsync();
            ids = (unit.Id, kept.Id, dropped.Id);
        });
        return ids;
    }

    private async Task WithContextAsync(Func<AppDbContext, Task> action)
    {
        using var scope = this.fixture.Factory.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
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

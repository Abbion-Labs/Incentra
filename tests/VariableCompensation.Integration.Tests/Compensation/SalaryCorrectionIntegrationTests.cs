using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Compensation;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Infrastructure.Persistence;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Compensation;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class SalaryCorrectionIntegrationTests
{
    private readonly IntegrationTestFixture fixture;

    public SalaryCorrectionIntegrationTests(IntegrationTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task SameDate_CorrectsTheSalaryInForce_AndNamesTheCompensationToRedo()
    {
        var employeeId = await this.SeedEmployeeAsync();
        var payroll = await this.SignInAsync(TestCredentials.PayrollEmail, TestCredentials.PayrollPassword);
        var url = $"/api/employee-salaries/{employeeId}";
        object Body(int points, string effectiveFrom, int? version) => new
        {
            points,
            salaryPerPoint = 100m,
            effectiveFrom,
            currency = "RSD",
            version,
        };

        var first = await PutAsync(payroll, url, Body(500, "2030-01-01", null));
        await this.SeedResultsAsync(employeeId);

        var corrected = await PutAsync(payroll, url, Body(550, "2030-01-01", first.GetProperty("version").GetInt32()));

        corrected.GetProperty("points").GetInt32().Should().Be(550);
        corrected.GetProperty("compensationYearsToRecalculate").EnumerateArray().Select(y => y.GetInt32())
            .Should().Equal(2030);
        corrected.GetProperty("finalizedCompensationYears").EnumerateArray().Select(y => y.GetInt32())
            .Should().Equal(2031);

        var history = await payroll.GetFromJsonAsync<JsonElement>($"{url}/history");
        history.EnumerateArray().Select(h => h.GetProperty("points").GetInt32()).Should().Equal(550);

        // An earlier date is still refused; a later one still starts a new salary.
        var earlier = await payroll.PutAsJsonAsync(url, Body(560, "2029-12-31", corrected.GetProperty("version").GetInt32()));
        earlier.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await earlier.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()
            .Should().Be(ErrorCodes.EffectiveDateMustBeAfterCurrent);

        var later = await PutAsync(payroll, url, Body(600, "2032-01-01", corrected.GetProperty("version").GetInt32()));
        later.GetProperty("compensationYearsToRecalculate").GetArrayLength().Should().Be(0);
        var fullHistory = await payroll.GetFromJsonAsync<JsonElement>($"{url}/history");
        fullHistory.GetArrayLength().Should().Be(2);
    }

    private static async Task<JsonElement> PutAsync(HttpClient client, string url, object body)
    {
        var response = await client.PutAsJsonAsync(url, body);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<long> SeedEmployeeAsync()
    {
        using var scope = this.fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var employee = new Employee
        {
            FirstName = "Plata",
            LastName = Guid.NewGuid().ToString("N")[..8],
            OrganizationUnitId = (await context.OrganizationUnits.FirstAsync()).Id,
            JobPositionId = (await context.JobPositions.FirstAsync()).Id,
            EducationLevelId = (await context.EducationLevels.FirstAsync()).Id,
            IsActive = true,
        };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        return employee.Id;
    }

    /// <summary>A calculation for 2030 still open, and one for 2031 already final.</summary>
    private async Task SeedResultsAsync(long employeeId)
    {
        using var scope = this.fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var employee = await context.Employees.SingleAsync(e => e.Id == employeeId);

        foreach (var (year, isFinal) in new[] { ((short)2030, false), ((short)2031, true) })
        {
            var parameters = await context.VariableCompensationParameters
                .FirstOrDefaultAsync(p => p.OrganizationUnitId == employee.OrganizationUnitId && p.Year == year);
            if (parameters is null)
            {
                parameters = new VariableCompensationParameters
                {
                    OrganizationUnitId = employee.OrganizationUnitId,
                    Year = year,
                    MonetaryPool = 1000m,
                };
                context.VariableCompensationParameters.Add(parameters);
                await context.SaveChangesAsync();
            }

            context.VariableCompensationResults.Add(new VariableCompensationResult
            {
                EmployeeId = employeeId,
                ParametersId = parameters.Id,
                Year = year,
                Points = 500,
                IsFinal = isFinal,
            });
        }

        await context.SaveChangesAsync();
    }

    private async Task<HttpClient> SignInAsync(string email, string password)
    {
        var client = this.fixture.Factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

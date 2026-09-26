using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Domain.Entities.Identity;
using VariableCompensation.Domain.Enums;
using VariableCompensation.Infrastructure.Persistence;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Employees;

/// <summary>Someone who has left the company is deactivated as an employee, and can no longer sign in.</summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class EmployeeDeactivationIntegrationTests
{
    private const string Password = "Otisao123!";

    private readonly IntegrationTestFixture fixture;

    public EmployeeDeactivationIntegrationTests(IntegrationTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task DeactivatingAnEmployee_ClosesTheirAccount_AndReactivatingDoesNotReopenIt()
    {
        var (employeeId, email) = await this.SeedEmployeeWithAccountAsync();
        var employee = await this.SignInAsync(email, Password);
        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);

        var deactivated = await UpdateAsync(admin, employeeId, isActive: false);
        deactivated.StatusCode.Should().Be(HttpStatusCode.OK, await deactivated.Content.ReadAsStringAsync());

        // Their open session ends at once, and they cannot sign in again.
        (await employee.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var login = await this.fixture.Factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var reactivated = await UpdateAsync(admin, employeeId, isActive: true);
        reactivated.StatusCode.Should().Be(HttpStatusCode.OK, await reactivated.Content.ReadAsStringAsync());

        using var scope = this.fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await context.Users.SingleAsync(u => u.Email == email)).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task TheAccountOfAnEmployeeWhoHasLeft_CannotBeReactivated()
    {
        var (employeeId, email) = await this.SeedEmployeeWithAccountAsync();
        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);
        (await UpdateAsync(admin, employeeId, isActive: false)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await SetAccountActiveAsync(admin, email, isActive: true);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()
            .Should().Be(ErrorCodes.AccountEmployeeInactive);
    }

    [Fact]
    public async Task TheAccountOfAnEvaluatorWithPeopleToRate_CannotBeDeactivated()
    {
        var (evaluatorId, email) = await this.SeedEmployeeWithAccountAsync(RoleCodes.Evaluator, isEvaluator: true);
        using (var scope = this.fixture.Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var subordinate = await this.NewEmployeeAsync(context, "Podređeni");
            subordinate.EvaluatorEmployeeId = evaluatorId;
            await context.SaveChangesAsync();
        }

        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);

        var response = await SetAccountActiveAsync(admin, email, isActive: false);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()
            .Should().Be(ErrorCodes.EmployeeHasSubordinates);
    }

    [Fact]
    public async Task TheAccountOfAControllerOfEvaluators_CannotBeDeactivated()
    {
        var (controllerId, email) = await this.SeedEmployeeWithAccountAsync(RoleCodes.Controller);
        var (evaluatorId, _) = await this.SeedEmployeeWithAccountAsync(RoleCodes.Evaluator, isEvaluator: true);
        using (var scope = this.fixture.Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var settings = await context.EvaluatorSettings.SingleAsync(s => s.EmployeeId == evaluatorId);
            settings.ControllerEmployeeId = controllerId;
            await context.SaveChangesAsync();
        }

        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);

        var response = await SetAccountActiveAsync(admin, email, isActive: false);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()
            .Should().Be(ErrorCodes.EmployeeControlsEvaluators);
    }

    [Fact]
    public async Task AnEmployeeWhoHasLeft_IsNotOfferedForANewSalary()
    {
        var (employeeId, _) = await this.SeedEmployeeWithAccountAsync();
        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);
        var payroll = await this.SignInAsync(TestCredentials.PayrollEmail, TestCredentials.PayrollPassword);

        (await OfferedForSalaryAsync(payroll)).Should().Contain(employeeId);

        (await UpdateAsync(admin, employeeId, isActive: false)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await OfferedForSalaryAsync(payroll)).Should().NotContain(employeeId);
    }

    [Fact]
    public async Task AnEvaluatorWhoHasLeft_CannotBeChosenForAnyone()
    {
        var (evaluatorId, _) = await this.SeedEmployeeWithAccountAsync(RoleCodes.Evaluator, isEvaluator: true);
        var (employeeId, _) = await this.SeedEmployeeWithAccountAsync();
        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);

        (await UpdateAsync(admin, evaluatorId, isActive: false)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await UpdateAsync(admin, employeeId, isActive: true, evaluatorEmployeeId: evaluatorId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()
            .Should().Be(ErrorCodes.EvaluatorInactive);
    }

    [Fact]
    public async Task AControllerWhoHasLeft_CannotBeChosenForAnEvaluator()
    {
        var (controllerId, _) = await this.SeedEmployeeWithAccountAsync(RoleCodes.Controller);
        var (evaluatorId, _) = await this.SeedEmployeeWithAccountAsync(RoleCodes.Evaluator, isEvaluator: true);
        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);

        (await UpdateAsync(admin, controllerId, isActive: false)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await admin.PutAsJsonAsync(
            $"/api/evaluator-settings/{evaluatorId}",
            new { controllerEmployeeId = controllerId, version = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()
            .Should().Be(ErrorCodes.ControllerInactive);
    }

    [Fact]
    public async Task AControllerOfEvaluators_CannotBeDeactivated()
    {
        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);

        var response = await UpdateAsync(admin, TestEmployeeIds.Controller, isActive: false);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()
            .Should().Be(ErrorCodes.EmployeeControlsEvaluators);

        // The controller still signs in and reviews.
        await this.SignInAsync(TestCredentials.ControllerEmail, TestCredentials.ControllerPassword);
    }

    [Fact]
    public async Task TheOnlyAdministrator_CannotBeDeactivatedAsAnEmployee()
    {
        long employeeId;
        using (var scope = this.fixture.Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var adminRoleUsers = await context.Users
                .Where(u => u.IsActive && u.UserRoles.Any(ur => ur.Role.Code == RoleCodes.Admin))
                .ToListAsync();
            adminRoleUsers.Should().ContainSingle("the test database has one administrator");

            var employee = await this.NewEmployeeAsync(context, "Admin");
            employee.UserId = adminRoleUsers[0].Id;
            await context.SaveChangesAsync();
            employeeId = employee.Id;
        }

        try
        {
            var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);

            var response = await UpdateAsync(admin, employeeId, isActive: false);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()
                .Should().Be(ErrorCodes.LastActiveAdministrator);
            (await admin.GetAsync("/api/users")).StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            // Other tests expect the administrator's account without an employee.
            using var scope = this.fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var employee = await context.Employees.SingleAsync(e => e.Id == employeeId);
            employee.UserId = null;
            await context.SaveChangesAsync();
        }
    }

    private static async Task<HttpResponseMessage> SetAccountActiveAsync(HttpClient admin, string email, bool isActive)
    {
        var users = await admin.GetFromJsonAsync<JsonElement>("/api/users");
        var user = users.EnumerateArray().Single(u => u.GetProperty("email").GetString() == email);
        return await admin.PutAsJsonAsync($"/api/users/{user.GetProperty("id").GetInt64()}", new
        {
            email,
            isActive,
            roleCodes = user.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToArray(),
            controllerEmployeeId = (long?)null,
            version = user.GetProperty("version").GetInt32(),
        });
    }

    private static async Task<List<long>> OfferedForSalaryAsync(HttpClient payroll)
    {
        var offered = new List<long>();
        for (var page = 1; ; page++)
        {
            var result = await payroll.GetFromJsonAsync<JsonElement>(
                $"/api/employee-salaries/employees-without-salary?page={page}&pageSize=100");
            var items = result.GetProperty("items").EnumerateArray().ToList();
            offered.AddRange(items.Select(i => i.GetProperty("employeeId").GetInt64()));
            if (items.Count < 100)
            {
                return offered;
            }
        }
    }

    private static async Task<HttpResponseMessage> UpdateAsync(
        HttpClient admin,
        long employeeId,
        bool isActive,
        long? evaluatorEmployeeId = null)
    {
        var current = await admin.GetFromJsonAsync<JsonElement>($"/api/employees/{employeeId}");
        var currentEvaluator = current.GetProperty("evaluatorEmployeeId");
        return await admin.PutAsJsonAsync($"/api/employees/{employeeId}", new
        {
            firstName = current.GetProperty("firstName").GetString(),
            lastName = current.GetProperty("lastName").GetString(),
            organizationUnitId = current.GetProperty("organizationUnitId").GetInt64(),
            jobPositionId = current.GetProperty("jobPositionId").GetInt64(),
            educationLevelId = current.GetProperty("educationLevelId").GetInt64(),
            evaluatorEmployeeId = evaluatorEmployeeId
                ?? (currentEvaluator.ValueKind == JsonValueKind.Null ? (long?)null : currentEvaluator.GetInt64()),
            hiredAt = (DateOnly?)null,
            isActive,
            version = current.GetProperty("version").GetInt32(),
        });
    }

    private async Task<(long EmployeeId, string Email)> SeedEmployeeWithAccountAsync(
        string roleCode = RoleCodes.Employee,
        bool isEvaluator = false)
    {
        using var scope = this.fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var role = await context.Roles.SingleAsync(r => r.Code == roleCode);
        var employee = await this.NewEmployeeAsync(context, "Otišao");
        var email = $"otisao-{Guid.NewGuid().ToString("N")[..12]}@local.dev";
        employee.User = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password, workFactor: 4),
            IsActive = true,
            UserRoles = { new UserRole { RoleId = role.Id } },
        };
        if (isEvaluator)
        {
            context.EvaluatorSettings.Add(new EvaluatorSettings { EmployeeId = employee.Id });
        }

        await context.SaveChangesAsync();
        return (employee.Id, email);
    }

    private async Task<Employee> NewEmployeeAsync(AppDbContext context, string firstName)
    {
        var employee = new Employee
        {
            FirstName = firstName,
            LastName = Guid.NewGuid().ToString("N")[..8],
            OrganizationUnitId = (await context.OrganizationUnits.FirstAsync()).Id,
            JobPositionId = (await context.JobPositions.FirstAsync()).Id,
            EducationLevelId = (await context.EducationLevels.FirstAsync()).Id,
            IsActive = true,
        };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        return employee;
    }

    private async Task<HttpClient> SignInAsync(string email, string password)
    {
        var client = this.fixture.Factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.StatusCode.Should().Be(HttpStatusCode.OK, $"{email} should sign in");
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

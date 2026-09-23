using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using VariableCompensation.Domain;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Concurrency;

/// <summary>
/// Every record edited through a form refuses an edit made from an outdated copy, and an edit that does not say
/// which copy it was made from.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class EditVersionIntegrationTests
{
    private readonly IntegrationTestFixture fixture;

    public EditVersionIntegrationTests(IntegrationTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task OrganizationUnit()
    {
        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);
        var name = Unique("Jedinica");
        var created = await CreateAsync(admin, "/api/organization-units", new { name });

        await AssertGuardedAsync(admin, $"/api/organization-units/{Id(created)}", Version(created), version => new
        {
            name,
            code = (string?)null,
            isActive = true,
            version,
        });
    }

    [Fact]
    public async Task JobPosition()
    {
        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);
        var name = Unique("Pozicija");
        var created = await CreateAsync(admin, "/api/job-positions", new { name, sortOrder = 9 });

        await AssertGuardedAsync(admin, $"/api/job-positions/{Id(created)}", Version(created), version => new
        {
            name,
            sortOrder = 9,
            isActive = true,
            version,
        });
    }

    [Fact]
    public async Task EducationLevel()
    {
        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);
        var name = Unique("Obrazovanje");
        var created = await CreateAsync(admin, "/api/education-levels", new { name, sortOrder = 9 });

        await AssertGuardedAsync(admin, $"/api/education-levels/{Id(created)}", Version(created), version => new
        {
            name,
            sortOrder = 9,
            isActive = true,
            version,
        });
    }

    [Fact]
    public async Task DescriptiveRating()
    {
        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);
        var ratings = await admin.GetFromJsonAsync<JsonElement>("/api/descriptive-ratings");
        var rating = ratings.EnumerateArray().Single(r => r.GetProperty("code").GetString() == "GOOD");

        // The same values again, so only the version moves.
        await AssertGuardedAsync(admin, $"/api/descriptive-ratings/{Id(rating)}", Version(rating), version => new
        {
            code = rating.GetProperty("code").GetString(),
            name = rating.GetProperty("name").GetString(),
            minAverage = rating.GetProperty("minAverage").GetDecimal(),
            maxAverage = rating.GetProperty("maxAverage").GetDecimal(),
            sortOrder = rating.GetProperty("sortOrder").GetInt32(),
            recommendedShare = rating.GetProperty("recommendedShare").GetDecimal(),
            isActive = true,
            version,
        });
    }

    [Fact]
    public async Task Employee()
    {
        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);
        var (created, body) = await this.CreateEmployeeAsync(admin);

        await AssertGuardedAsync(admin, $"/api/employees/{Id(created)}", Version(created), version => body(version));
    }

    [Fact]
    public async Task EmployeeUserLink()
    {
        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);
        var (created, _) = await this.CreateEmployeeAsync(admin);

        await AssertGuardedAsync(admin, $"/api/employees/{Id(created)}/user", Version(created), version => new
        {
            userId = (long?)null,
            version,
        });
    }

    [Fact]
    public async Task User()
    {
        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);
        var email = $"{Unique("korisnik")}@local.dev";
        var created = await CreateAsync(admin, "/api/auth/register", new
        {
            email,
            password = "Korisnik123!",
            roleCodes = new[] { "EMPLOYEE" },
        });
        var users = await admin.GetFromJsonAsync<JsonElement>("/api/users");
        var user = users.EnumerateArray().Single(u => u.GetProperty("id").GetInt64() == Id(created));

        await AssertGuardedAsync(admin, $"/api/users/{Id(created)}", Version(user), version => new
        {
            email,
            isActive = true,
            roleCodes = new[] { "EMPLOYEE" },
            version,
        });
    }

    [Fact]
    public async Task EvaluatorSettings()
    {
        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);
        var settings = await admin.GetFromJsonAsync<JsonElement>($"/api/evaluator-settings/{TestEmployeeIds.Evaluator}");

        // Keeps the seeded controller: only the version moves.
        await AssertGuardedAsync(admin, $"/api/evaluator-settings/{TestEmployeeIds.Evaluator}", Version(settings), version => new
        {
            controllerEmployeeId = TestEmployeeIds.Controller,
            version,
        });
    }

    [Fact]
    public async Task CompensationParameters()
    {
        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);
        var unit = await CreateAsync(admin, "/api/organization-units", new { name = Unique("Fond") });
        var payroll = await this.SignInAsync(TestCredentials.PayrollEmail, TestCredentials.PayrollPassword);
        var created = await CreateAsync(payroll, "/api/compensation-parameters", new
        {
            organizationUnitId = Id(unit),
            year = 2023,
            monetaryPool = 1000m,
            currency = "RSD",
            acceptablePerformanceRating = 3m,
            dependencyWeight = 0.5m,
            exponent = 1m,
            allowNegativeVariable = false,
        });

        await AssertGuardedAsync(payroll, $"/api/compensation-parameters/{Id(created)}", Version(created), version => new
        {
            monetaryPool = 2000m,
            currency = "RSD",
            acceptablePerformanceRating = 3m,
            dependencyWeight = 0.5m,
            exponent = 1m,
            allowNegativeVariable = false,
            isActive = true,
            version,
        });
    }

    [Fact]
    public async Task Salary()
    {
        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);
        var (employee, _) = await this.CreateEmployeeAsync(admin);
        var payroll = await this.SignInAsync(TestCredentials.PayrollEmail, TestCredentials.PayrollPassword);
        var url = $"/api/employee-salaries/{Id(employee)}";
        object Body(int? version) => new
        {
            points = 300,
            salaryPerPoint = 100m,
            effectiveFrom = "2026-01-01",
            currency = "RSD",
            version,
        };

        // The first salary has nothing to have been read from.
        var first = await payroll.PutAsJsonAsync(url, Body(null));
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var version = Version(await first.Content.ReadFromJsonAsync<JsonElement>());

        await AssertGuardedAsync(payroll, url, version, Body);
    }

    /// <summary>No version is refused, the current one goes through, and the same one again is outdated.</summary>
    private static async Task AssertGuardedAsync(HttpClient client, string url, int currentVersion, Func<int?, object> body)
    {
        await ShouldFailAsync(await client.PutAsJsonAsync(url, body(null)), ErrorCodes.VersionRequired);

        var edited = await client.PutAsJsonAsync(url, body(currentVersion));
        edited.StatusCode.Should().Be(HttpStatusCode.OK, await edited.Content.ReadAsStringAsync());
        Version(await edited.Content.ReadFromJsonAsync<JsonElement>()).Should().Be(currentVersion + 1);

        await ShouldFailAsync(await client.PutAsJsonAsync(url, body(currentVersion)), ErrorCodes.ConcurrencyConflict);
    }

    private static async Task ShouldFailAsync(HttpResponseMessage response, string errorCode)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString().Should().Be(errorCode);
    }

    private static async Task<JsonElement> CreateAsync(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<(JsonElement Created, Func<int?, object> Body)> CreateEmployeeAsync(HttpClient admin)
    {
        var orgUnits = await admin.GetFromJsonAsync<JsonElement>("/api/organization-units");
        var positions = await admin.GetFromJsonAsync<JsonElement>("/api/job-positions");
        var educations = await admin.GetFromJsonAsync<JsonElement>("/api/education-levels");
        var lastName = Unique("Verzija");
        var fields = new
        {
            firstName = "Izmena",
            lastName,
            organizationUnitId = orgUnits[0].GetProperty("id").GetInt64(),
            jobPositionId = positions[0].GetProperty("id").GetInt64(),
            educationLevelId = educations[0].GetProperty("id").GetInt64(),
            evaluatorEmployeeId = (long?)null,
        };

        var created = await CreateAsync(admin, "/api/employees", fields);
        object Body(int? version) => new
        {
            fields.firstName,
            fields.lastName,
            fields.organizationUnitId,
            fields.jobPositionId,
            fields.educationLevelId,
            fields.evaluatorEmployeeId,
            isActive = true,
            version,
        };

        return (created, Body);
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

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..(prefix.Length + 9)];

    private static long Id(JsonElement record) => record.GetProperty("id").GetInt64();

    private static int Version(JsonElement record) => record.GetProperty("version").GetInt32();
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VariableCompensation.Domain;
using VariableCompensation.Infrastructure.Persistence;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Auth;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class LastAdministratorIntegrationTests
{
    private readonly IntegrationTestFixture fixture;

    public LastAdministratorIntegrationTests(IntegrationTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Theory]
    [InlineData(false, "ADMIN")]
    [InlineData(true, "EMPLOYEE")]
    public async Task TheOnlyAdministrator_CannotDeactivateThemselvesOrDropTheRole(bool isActive, string roleCode)
    {
        long adminId;
        using (var scope = this.fixture.Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            adminId = await context.Users.Where(u => u.Email == TestCredentials.AdminEmail).Select(u => u.Id).SingleAsync();
        }

        var admin = this.fixture.Factory.CreateClient();
        var login = await admin.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestCredentials.AdminEmail, password = TestCredentials.AdminPassword });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await admin.PutAsJsonAsync($"/api/users/{adminId}", new
        {
            email = TestCredentials.AdminEmail,
            isActive,
            roleCodes = new[] { roleCode },
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()
            .Should().Be(ErrorCodes.LastActiveAdministrator);

        // Still an administrator, and still signed in.
        (await admin.GetAsync("/api/users")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

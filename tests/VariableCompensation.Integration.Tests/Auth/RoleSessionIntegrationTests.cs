using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Enums;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Auth;

/// <summary>
/// A session works in one of the user's roles, and switching starts a new session in another one. The test
/// creates its own account with two roles, so the shared seeded users are left alone.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class RoleSessionIntegrationTests
{
    private const string CookieName = "vc_refresh";
    private const string SalariesPath = "/api/employee-salaries/employees-without-salary";

    private readonly HttpClient client;

    public RoleSessionIntegrationTests(IntegrationTestFixture fixture)
    {
        // Cookies are handled by hand so a session can be used after another has replaced it.
        this.client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
    }

    [Fact]
    public async Task SessionWorksInOneRole_AndSwitchingReplacesIt()
    {
        var (email, password) = await this.CreateUserWithTwoRolesAsync();

        // Signing in starts in the first role by priority, here the employee one.
        var employeeSession = await this.SignInAsync(email, password);
        employeeSession.ActiveRole.Should().Be(RoleCodes.Employee);
        (await this.SendAsync(HttpMethod.Get, SalariesPath, employeeSession.AccessToken))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden, "the session carries only the employee role");

        var notAssigned = await this.SelectRoleAsync(employeeSession.AccessToken, RoleCodes.Admin);
        notAssigned.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await notAssigned.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()
            .Should().Be(ErrorCodes.RoleNotAssigned);

        var selected = await this.SelectRoleAsync(employeeSession.AccessToken, RoleCodes.Payroll);
        selected.StatusCode.Should().Be(HttpStatusCode.OK);
        var payrollSession = ReadSession(selected, await selected.Content.ReadFromJsonAsync<JsonElement>());
        payrollSession.ActiveRole.Should().Be(RoleCodes.Payroll);

        (await this.SendAsync(HttpMethod.Get, SalariesPath, payrollSession.AccessToken))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await (await this.SendAsync(HttpMethod.Get, "/api/auth/me", payrollSession.AccessToken))
            .Content.ReadFromJsonAsync<JsonElement>();
        profile.GetProperty("activeRole").GetString().Should().Be(RoleCodes.Payroll);

        // The session it replaced is over, both its tokens.
        (await this.SendAsync(HttpMethod.Get, "/api/auth/me", employeeSession.AccessToken))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await this.RefreshAsync(employeeSession.RefreshToken))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Refreshing keeps the role, and so does signing in again.
        var refreshed = await this.RefreshAsync(payrollSession.RefreshToken);
        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await refreshed.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("user").GetProperty("activeRole").GetString().Should().Be(RoleCodes.Payroll);

        (await this.SignInAsync(email, password)).ActiveRole.Should().Be(RoleCodes.Payroll);
    }

    private async Task<(string Email, string Password)> CreateUserWithTwoRolesAsync()
    {
        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);
        var email = $"role-session-{Guid.NewGuid():N}@local.dev";
        const string password = "Several123!";

        var response = await this.SendAsync(
            HttpMethod.Post,
            "/api/auth/register",
            admin.AccessToken,
            new { email, password, roleCodes = new[] { RoleCodes.Employee, RoleCodes.Payroll } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (email, password);
    }

    private async Task<(string AccessToken, string RefreshToken, string? ActiveRole)> SignInAsync(
        string email,
        string password)
    {
        var response = await this.client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return ReadSession(response, await response.Content.ReadFromJsonAsync<JsonElement>());
    }

    private Task<HttpResponseMessage> SelectRoleAsync(string accessToken, string roleCode) =>
        this.SendAsync(HttpMethod.Post, "/api/auth/select-role", accessToken, new { roleCode });

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"{CookieName}={refreshToken}");
        return this.client.SendAsync(request);
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string accessToken, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return this.client.SendAsync(request);
    }

    private static (string AccessToken, string RefreshToken, string? ActiveRole) ReadSession(
        HttpResponseMessage response,
        JsonElement body) =>
        (body.GetProperty("accessToken").GetString()!,
            RefreshCookieValue(response),
            body.GetProperty("user").GetProperty("activeRole").GetString());

    private static string RefreshCookieValue(HttpResponseMessage response)
    {
        var setCookie = response.Headers.GetValues("Set-Cookie")
            .First(value => value.StartsWith($"{CookieName}=", StringComparison.Ordinal));

        return setCookie[(CookieName.Length + 1)..].Split(';')[0];
    }
}

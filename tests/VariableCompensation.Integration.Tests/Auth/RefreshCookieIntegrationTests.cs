using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Auth;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class RefreshCookieIntegrationTests
{
    private const string CookieName = "vc_refresh";

    private readonly HttpClient client;

    public RefreshCookieIntegrationTests(IntegrationTestFixture fixture)
    {
        // Cookies are handled by hand so the tests can assert on the raw Set-Cookie header.
        this.client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
    }

    [Fact]
    public async Task Login_SetsHttpOnlyStrictCookie_AndKeepsTheRefreshTokenOutOfTheBody()
    {
        var response = await this.LoginAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var setCookie = FindRefreshCookie(response);
        setCookie.Should().NotBeNull();
        setCookie!.ToLowerInvariant().Should()
            .Contain("httponly").And
            .Contain("samesite=strict").And
            .Contain("path=/api/auth");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("refreshToken", out _).Should().BeFalse();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// No environment here overrides RefreshCookie:Secure, so this asserts the default the deployed
    /// app runs with. Only appsettings.Development.json turns it off, for local HTTP.
    /// </summary>
    [Fact]
    public async Task RefreshCookie_IsSecureByDefault()
    {
        var setCookie = FindRefreshCookie(await this.LoginAsync())!;

        setCookie.ToLowerInvariant().Should().Contain("secure");
    }

    /// <summary>
    /// Registration returns an AuthResponse for the *created* user. Setting a cookie from it would
    /// hand the admin who is creating the account the new user's session.
    /// </summary>
    [Fact]
    public async Task Register_DoesNotSetARefreshCookieForTheAdminCreatingTheUser()
    {
        var admin = this.client;
        var login = await admin.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestCredentials.AdminEmail,
            password = TestCredentials.AdminPassword,
        });
        var accessToken = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = JsonContent.Create(new
            {
                email = $"cookie-test-{Guid.NewGuid():N}@local.dev",
                password = "Register123!",
                roleCodes = new[] { "EMPLOYEE" },
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await admin.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        FindRefreshCookie(response).Should().BeNull();
        (await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("refreshToken", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task Refresh_WithCookie_ReturnsANewAccessTokenAndRotatesTheCookie()
    {
        var firstToken = CookieValue(FindRefreshCookie(await this.LoginAsync())!);

        var refreshed = await this.PostWithCookieAsync("/api/auth/refresh", firstToken);

        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await refreshed.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        body.TryGetProperty("refreshToken", out _).Should().BeFalse();

        var rotatedToken = CookieValue(FindRefreshCookie(refreshed)!);
        rotatedToken.Should().NotBe(firstToken);

        (await this.PostWithCookieAsync("/api/auth/refresh", firstToken)).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized, "a rotated token must not be reusable");
        (await this.PostWithCookieAsync("/api/auth/refresh", rotatedToken)).StatusCode
            .Should().Be(HttpStatusCode.OK, "the rotated token is the valid one");
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized()
    {
        var response = await this.client.PostAsync("/api/auth/refresh", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithTheTokenInTheBody_IsStillAccepted()
    {
        var token = Uri.UnescapeDataString(CookieValue(FindRefreshCookie(await this.LoginAsync())!));

        var response = await this.client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = token });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_RevokesTheTokenAndClearsTheCookie()
    {
        var token = CookieValue(FindRefreshCookie(await this.LoginAsync())!);

        var logout = await this.PostWithCookieAsync("/api/auth/logout", token);

        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        FindRefreshCookie(logout)!.ToLowerInvariant().Should().Contain("expires=thu, 01 jan 1970");

        (await this.PostWithCookieAsync("/api/auth/refresh", token)).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithoutCookie_StillSucceeds()
    {
        var response = await this.client.PostAsync("/api/auth/logout", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private Task<HttpResponseMessage> LoginAsync() =>
        this.client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestCredentials.EvaluatorEmail,
            password = TestCredentials.EvaluatorPassword,
        });

    private Task<HttpResponseMessage> PostWithCookieAsync(string path, string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("Cookie", $"{CookieName}={refreshToken}");
        return this.client.SendAsync(request);
    }

    private static string? FindRefreshCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith($"{CookieName}=", StringComparison.Ordinal))
            : null;

    private static string CookieValue(string setCookie) =>
        setCookie[(CookieName.Length + 1)..].Split(';')[0];
}

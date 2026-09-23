using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using VariableCompensation.Application.Auth.Commands.Login;
using VariableCompensation.Infrastructure.Persistence;
using VariableCompensation.Integration.Tests.Infrastructure;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Auth;

/// <summary>
/// Access tokens are checked against their session on every request, and refresh tokens rotate within it.
/// Tests that change an account work on one they create, so the shared seeded users are left alone.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class SessionIntegrationTests
{
    private const string CookieName = "vc_refresh";

    private readonly CustomWebApplicationFactory factory;
    private readonly HttpClient client;

    public SessionIntegrationTests(IntegrationTestFixture fixture)
    {
        this.factory = fixture.Factory;
        this.client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
    }

    [Fact]
    public async Task AccessToken_StopsWorking_OnceItsSessionSignsOut()
    {
        var session = await this.SignInAsync(TestCredentials.EvaluatorEmail, TestCredentials.EvaluatorPassword);
        (await this.GetMeAsync(session.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);

        await this.PostWithCookieAsync("/api/auth/logout", session.RefreshToken);

        (await this.GetMeAsync(session.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AccessToken_StopsWorking_OnceTheAccountIsDeactivated()
    {
        var user = await this.CreateUserAsync();
        var session = await this.SignInAsync(user.Email, user.Password);
        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);

        var deactivate = await this.SendAsync(
            HttpMethod.Put,
            $"/api/users/{user.Id}",
            admin.AccessToken,
            // A new account has not been edited yet.
            new { email = user.Email, isActive = false, roleCodes = new[] { "EMPLOYEE" }, version = 0 });

        deactivate.StatusCode.Should().Be(HttpStatusCode.OK);
        (await this.GetMeAsync(session.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await this.PostWithCookieAsync("/api/auth/refresh", session.RefreshToken)).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_SignsOutOtherDevices_AndKeepsThisOne()
    {
        var user = await this.CreateUserAsync();
        var thisDevice = await this.SignInAsync(user.Email, user.Password);
        var otherDevice = await this.SignInAsync(user.Email, user.Password);

        var change = await this.SendAsync(
            HttpMethod.Put,
            "/api/auth/change-password",
            thisDevice.AccessToken,
            new { currentPassword = user.Password, newPassword = "Changed123!" });

        change.StatusCode.Should().Be(HttpStatusCode.OK);
        (await this.GetMeAsync(thisDevice.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await this.PostWithCookieAsync("/api/auth/refresh", thisDevice.RefreshToken)).StatusCode
            .Should().Be(HttpStatusCode.OK);

        (await this.GetMeAsync(otherDevice.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await this.PostWithCookieAsync("/api/auth/refresh", otherDevice.RefreshToken)).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExchangedRefreshToken_IsAcceptedAgainWithinTheGracePeriod_AndEndsTheSessionAfterIt()
    {
        var session = await this.SignInAsync(TestCredentials.EvaluatorEmail, TestCredentials.EvaluatorPassword);
        var exchange = await this.PostWithCookieAsync("/api/auth/refresh", session.RefreshToken);
        exchange.StatusCode.Should().Be(HttpStatusCode.OK);
        var successor = CookieValue(FindRefreshCookie(exchange)!);

        // As if the response had been lost: the browser still holds the token it has just exchanged.
        (await this.PostWithCookieAsync("/api/auth/refresh", session.RefreshToken)).StatusCode
            .Should().Be(HttpStatusCode.OK);

        await this.BackdateExchangeAsync(session.RefreshToken, TimeSpan.FromMinutes(5));

        (await this.PostWithCookieAsync("/api/auth/refresh", session.RefreshToken)).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
        (await this.PostWithCookieAsync("/api/auth/refresh", successor)).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized, "a replay ends the session for whoever holds the successor too");
        (await this.GetMeAsync(session.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConcurrentRefreshesWithOneToken_BothSucceed_AndTheSessionCarriesOn()
    {
        var session = await this.SignInAsync(TestCredentials.EvaluatorEmail, TestCredentials.EvaluatorPassword);

        var responses = await Task.WhenAll(
            this.PostWithCookieAsync("/api/auth/refresh", session.RefreshToken),
            this.PostWithCookieAsync("/api/auth/refresh", session.RefreshToken));

        responses.Select(r => r.StatusCode).Should().AllBeEquivalentTo(HttpStatusCode.OK);
        var successors = responses.Select(r => CookieValue(FindRefreshCookie(r)!)).ToList();
        successors.Should().OnlyHaveUniqueItems();
        foreach (var successor in successors)
        {
            (await this.PostWithCookieAsync("/api/auth/refresh", successor)).StatusCode
                .Should().Be(HttpStatusCode.OK);
        }
    }

    /// <summary>
    /// What tokens issued before sessions existed look like: validly signed, but tied to no sign-in.
    /// </summary>
    [Fact]
    public async Task AccessTokenWithoutASession_IsRejected()
    {
        var configuration = this.factory.Services.GetRequiredService<IConfiguration>();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!));
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: [new Claim(JwtRegisteredClaimNames.Sub, "1"), new Claim(ClaimTypes.Role, "ADMIN")],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));

        (await this.GetMeAsync(token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<(string AccessToken, string RefreshToken)> SignInAsync(string email, string password)
    {
        var response = await this.client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("accessToken").GetString()!, CookieValue(FindRefreshCookie(response)!));
    }

    private async Task<(long Id, string Email, string Password)> CreateUserAsync()
    {
        var admin = await this.SignInAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword);
        var email = $"session-test-{Guid.NewGuid():N}@local.dev";
        const string password = "Session123!";

        var response = await this.SendAsync(
            HttpMethod.Post,
            "/api/auth/register",
            admin.AccessToken,
            new { email, password, roleCodes = new[] { "EMPLOYEE" } });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (profile.GetProperty("id").GetInt64(), email, password);
    }

    /// <summary>
    /// Moves a token's exchange into the past, as if the grace period had run out since.
    /// </summary>
    private async Task BackdateExchangeAsync(string refreshToken, TimeSpan by)
    {
        using var scope = this.factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenHash = LoginCommandHandler.HashToken(refreshToken);

        var token = await context.RefreshTokens.SingleAsync(rt => rt.TokenHash == tokenHash);
        token.RevokedAt = token.RevokedAt!.Value - by;
        await context.SaveChangesAsync();
    }

    private Task<HttpResponseMessage> GetMeAsync(string accessToken) =>
        this.SendAsync(HttpMethod.Get, "/api/auth/me", accessToken);

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

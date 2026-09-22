using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Infrastructure;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class SecurityHeadersIntegrationTests
{
    private readonly HttpClient client;

    public SecurityHeadersIntegrationTests(IntegrationTestFixture fixture)
    {
        this.client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task ApiResponses_CarryTheSecurityHeaders()
    {
        var response = await this.client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestCredentials.EvaluatorEmail,
            password = TestCredentials.EvaluatorPassword,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Header(response, "X-Content-Type-Options").Should().Be("nosniff");
        Header(response, "X-Frame-Options").Should().Be("DENY");
        Header(response, "Referrer-Policy").Should().Be("no-referrer");
        Header(response, "Content-Security-Policy").Should().Be("default-src 'none'; frame-ancestors 'none'");
        response.Headers.CacheControl!.NoStore.Should().BeTrue("the answer carries an access token");
    }

    [Fact]
    public async Task RejectedRequests_CarryThemAsWell()
    {
        var response = await this.client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        Header(response, "X-Content-Type-Options").Should().Be("nosniff");
        Header(response, "Content-Security-Policy").Should().NotBeNull();
    }

    /// <summary>
    /// Outside the API the app also serves Swagger UI, which runs scripts of its own.
    /// </summary>
    [Fact]
    public async Task OutsideTheApi_OnlyTheHeadersThatFitEverything()
    {
        var response = await this.client.GetAsync("/health");

        Header(response, "X-Content-Type-Options").Should().Be("nosniff");
        Header(response, "Content-Security-Policy").Should().BeNull();
    }

    private static string? Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? string.Join(", ", values) : null;
}

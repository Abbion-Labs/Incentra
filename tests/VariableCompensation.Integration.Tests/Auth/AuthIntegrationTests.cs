using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Auth;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class AuthIntegrationTests
{
    private readonly HttpClient client;

    public AuthIntegrationTests(IntegrationTestFixture fixture)
    {
        this.client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var response = await this.client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestCredentials.EvaluatorEmail,
            password = TestCredentials.EvaluatorPassword,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await this.client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithToken_ReturnsProfile()
    {
        var login = await this.client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestCredentials.EvaluatorEmail,
            password = TestCredentials.EvaluatorPassword,
        });
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await this.client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<JsonElement>();
        profile.GetProperty("email").GetString().Should().Be(TestCredentials.EvaluatorEmail);
    }
}

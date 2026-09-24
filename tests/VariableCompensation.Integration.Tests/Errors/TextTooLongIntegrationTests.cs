using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using VariableCompensation.Domain;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Errors;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class TextTooLongIntegrationTests
{
    private readonly IntegrationTestFixture fixture;

    public TextTooLongIntegrationTests(IntegrationTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task TextLongerThanItsColumn_IsRefusedWithAClearError()
    {
        var client = this.fixture.Factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestCredentials.AdminEmail, password = TestCredentials.AdminPassword });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Job position names hold at most 100 characters.
        var response = await client.PostAsJsonAsync("/api/job-positions", new { name = new string('a', 101), sortOrder = 99 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()
            .Should().Be(ErrorCodes.TextTooLong);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using VariableCompensation.Domain;
using VariableCompensation.Testing.Common;

namespace VariableCompensation.Integration.Tests.Employees;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class EmployeeAvatarIntegrationTests
{
    private readonly IntegrationTestFixture fixture;

    public EmployeeAvatarIntegrationTests(IntegrationTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task FailedUpload_KeepsTheCurrentPicture_AndANewOneReplacesIt()
    {
        var client = await this.CreateClientAsync(TestCredentials.EmployeeEmail, TestCredentials.EmployeePassword);
        var path = $"/api/employees/{TestEmployeeIds.Employee}/avatar";

        var first = await UploadAsync(client, path, "image/png");
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstUrl = await AvatarUrlAsync(first);

        var unsupported = await UploadAsync(client, path, "image/gif");
        unsupported.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorAsync(unsupported)).Should().Be(ErrorCodes.ImageTypeUnsupported);
        (await this.CurrentAvatarUrlAsync(client)).Should().Be(firstUrl);
        (await client.GetAsync(firstUrl)).StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await UploadAsync(client, path, "image/png");
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondUrl = await AvatarUrlAsync(second);
        secondUrl.Should().NotBe(firstUrl);
        (await client.GetAsync(secondUrl)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync(firstUrl)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var removed = await client.DeleteAsync(path);
        removed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await this.CurrentAvatarUrlAsync(client)).Should().BeNull();
        (await client.GetAsync(secondUrl)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UploadWithoutAFile_IsRefusedWithACodeTheAppTranslates()
    {
        var client = await this.CreateClientAsync(TestCredentials.EmployeeEmail, TestCredentials.EmployeePassword);
        using var form = new MultipartFormDataContent { { new StringContent("x"), "other" } };

        var response = await client.PostAsync($"/api/employees/{TestEmployeeIds.Employee}/avatar", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorAsync(response)).Should().Be(ErrorCodes.ImageRequired);
    }

    private static async Task<string?> ErrorAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString();

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, string path, string contentType)
    {
        var file = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        using var form = new MultipartFormDataContent { { file, "file", "avatar" } };
        return await client.PostAsync(path, form);
    }

    private static async Task<string?> AvatarUrlAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("avatarUrl").GetString();

    private async Task<string?> CurrentAvatarUrlAsync(HttpClient client) =>
        await AvatarUrlAsync(await client.GetAsync($"/api/employees/{TestEmployeeIds.Employee}"));

    private async Task<HttpClient> CreateClientAsync(string email, string password)
    {
        var client = this.fixture.Factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

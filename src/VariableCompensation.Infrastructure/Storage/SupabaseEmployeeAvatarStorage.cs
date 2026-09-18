using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using VariableCompensation.Application.Abstractions.Storage;

namespace VariableCompensation.Infrastructure.Storage;

public sealed class SupabaseEmployeeAvatarStorage : IEmployeeAvatarStorage
{
    public const string HttpClientName = "SupabaseEmployeeAvatarStorage";

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    private readonly IHttpClientFactory httpClientFactory;
    private readonly SupabaseEmployeeAvatarStorageOptions options;

    public SupabaseEmployeeAvatarStorage(
        IHttpClientFactory httpClientFactory,
        IOptions<SupabaseEmployeeAvatarStorageOptions> options)
    {
        this.httpClientFactory = httpClientFactory;
        this.options = options.Value;
    }

    public async Task<string> SaveAsync(
        long employeeId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException("Unsupported image type. Use JPEG, PNG or WebP.");
        }

        var extension = contentType switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg",
        };

        var objectPath = $"employees/{employeeId}/{Guid.NewGuid():N}{extension}";
        var requestPath = this.BuildObjectRequestPath(objectPath);

        using var request = new HttpRequestMessage(HttpMethod.Post, requestPath);
        request.Headers.TryAddWithoutValidation("x-upsert", "false");
        request.Content = new StreamContent(content);
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        using var response = await this.httpClientFactory
            .CreateClient(HttpClientName)
            .SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Supabase avatar upload failed with status {(int)response.StatusCode} ({response.ReasonPhrase}).");
        }

        return this.BuildPublicUrl(objectPath);
    }

    public async Task DeleteIfExistsAsync(string? avatarUrl, CancellationToken cancellationToken)
    {
        if (!this.TryGetObjectPath(avatarUrl, out var objectPath))
        {
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Delete, this.BuildObjectRequestPath(objectPath));
        using var response = await this.httpClientFactory
            .CreateClient(HttpClientName)
            .SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Supabase avatar delete failed with status {(int)response.StatusCode} ({response.ReasonPhrase}).");
        }
    }

    private string BuildObjectRequestPath(string objectPath) =>
        $"storage/v1/object/{Uri.EscapeDataString(this.options.Bucket)}/{EncodePath(objectPath)}";

    private string BuildPublicUrl(string objectPath) =>
        $"{this.options.Url.TrimEnd('/')}/storage/v1/object/public/{Uri.EscapeDataString(this.options.Bucket)}/{EncodePath(objectPath)}";

    private bool TryGetObjectPath(string? avatarUrl, out string objectPath)
    {
        objectPath = string.Empty;

        if (string.IsNullOrWhiteSpace(avatarUrl)
            || !Uri.TryCreate(avatarUrl, UriKind.Absolute, out var avatarUri)
            || !Uri.TryCreate(this.options.Url, UriKind.Absolute, out var supabaseUri)
            || !string.Equals(avatarUri.Scheme, supabaseUri.Scheme, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(avatarUri.Host, supabaseUri.Host, StringComparison.OrdinalIgnoreCase)
            || avatarUri.Port != supabaseUri.Port)
        {
            return false;
        }

        var decodedPath = Uri.UnescapeDataString(avatarUri.AbsolutePath);
        var expectedPrefix = $"/storage/v1/object/public/{this.options.Bucket}/";

        if (!decodedPath.StartsWith(expectedPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        objectPath = decodedPath[expectedPrefix.Length..];
        return !string.IsNullOrWhiteSpace(objectPath);
    }

    private static string EncodePath(string path) =>
        string.Join(
            "/",
            path.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));
}

using Microsoft.Extensions.Hosting;
using VariableCompensation.Application.Abstractions.Storage;

namespace VariableCompensation.Infrastructure.Storage;

public sealed class LocalEmployeeAvatarStorage : IEmployeeAvatarStorage
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    private readonly string avatarDirectory;
    private readonly string avatarRequestPath;

    public LocalEmployeeAvatarStorage(IHostEnvironment environment)
    {
        this.avatarDirectory = Path.Combine(environment.ContentRootPath, "uploads", "avatars");
        this.avatarRequestPath = "/avatars";
        Directory.CreateDirectory(this.avatarDirectory);
    }

    public static string AvatarsDirectory(IHostEnvironment environment) =>
        Path.Combine(environment.ContentRootPath, "uploads", "avatars");

    public async Task<string> SaveAsync(long employeeId, Stream content, string contentType, CancellationToken cancellationToken)
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

        foreach (var existing in Directory.EnumerateFiles(this.avatarDirectory, $"{employeeId}.*"))
        {
            File.Delete(existing);
        }

        var fileName = $"{employeeId}{extension}";
        var fullPath = Path.Combine(this.avatarDirectory, fileName);
        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);

        var version = File.GetLastWriteTimeUtc(fullPath).Ticks;
        return $"{this.avatarRequestPath}/{fileName}?v={version}";
    }

    public Task DeleteIfExistsAsync(string? avatarUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
        {
            return Task.CompletedTask;
        }

        var fileName = Path.GetFileName(avatarUrl.Split('?', 2)[0]);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Task.CompletedTask;
        }

        var fullPath = Path.Combine(this.avatarDirectory, fileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}

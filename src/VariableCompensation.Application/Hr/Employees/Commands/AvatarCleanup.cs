using Microsoft.Extensions.Logging;
using VariableCompensation.Application.Abstractions.Storage;

namespace VariableCompensation.Application.Hr.Employees.Commands;

internal static class AvatarCleanup
{
    /// <summary>
    /// Removes a picture nobody points at any more. The change it follows is already saved, so a failure here only
    /// leaves a stray file behind and is logged rather than reported.
    /// </summary>
    internal static async Task DeleteQuietlyAsync(IEmployeeAvatarStorage storage, string? avatarUrl, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
        {
            return;
        }

        try
        {
            await storage.DeleteIfExistsAsync(avatarUrl, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete the avatar {AvatarUrl}", avatarUrl);
        }
    }
}

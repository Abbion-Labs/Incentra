using System.Text.Json.Serialization;

namespace VariableCompensation.Application.Auth.Models;

public sealed class AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Returned to the API layer so it can set the refresh cookie, but never serialized into the
    /// response body: scripts on the page must not be able to read it.
    /// </summary>
    [JsonIgnore]
    public string RefreshToken { get; init; } = string.Empty;

    public DateTime AccessTokenExpiresAt { get; init; }

    public DateTime RefreshTokenExpiresAt { get; init; }

    public UserProfileResponse User { get; init; } = null!;
}

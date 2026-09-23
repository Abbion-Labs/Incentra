namespace VariableCompensation.Api.Contracts.Auth;

public sealed class UpdateAdminUserRequest
{
    /// <summary>The version the edit was made from. Required: an edit from an outdated copy is refused.</summary>
    public int? Version { get; init; }

    public string Email { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;

    public IReadOnlyList<string> RoleCodes { get; init; } = Array.Empty<string>();

    public long? ControllerEmployeeId { get; init; }
}

public sealed class ResetAdminUserPasswordRequest
{
    public string NewPassword { get; init; } = string.Empty;
}

namespace VariableCompensation.Api.Contracts.Auth;

public sealed class UpdateAdminUserRequest
{
    public string Email { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;

    public IReadOnlyList<string> RoleCodes { get; init; } = Array.Empty<string>();

    public long? ControllerEmployeeId { get; init; }
}

public sealed class ResetAdminUserPasswordRequest
{
    public string NewPassword { get; init; } = string.Empty;
}

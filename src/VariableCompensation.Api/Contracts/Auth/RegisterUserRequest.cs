namespace VariableCompensation.Api.Contracts.Auth;

public sealed class RegisterUserRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public IReadOnlyList<string> RoleCodes { get; init; } = Array.Empty<string>();

    public string RoleCode { get; init; } = string.Empty;
}

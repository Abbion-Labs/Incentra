namespace VariableCompensation.Api.Contracts.Auth;

public sealed class RegisterUserRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public IReadOnlyList<string> RoleCodes { get; init; } = Array.Empty<string>();

    public string RoleCode { get; init; } = string.Empty;

    /// <summary>The employee the account belongs to. Required for the Employee, Evaluator and Controller roles.</summary>
    public long? EmployeeId { get; init; }

    /// <summary>The controller of a new evaluator; none for an evaluator whose evaluations need no review.</summary>
    public long? ControllerEmployeeId { get; init; }
}

namespace VariableCompensation.Application.Hr.Models;

public sealed class EmployeeResponse
{
    public long Id { get; init; }

    /// <summary>Sent back with an edit; see <c>IVersioned</c>.</summary>
    public int Version { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public long OrganizationUnitId { get; init; }

    public string OrganizationUnitName { get; init; } = string.Empty;

    public long JobPositionId { get; init; }

    public string JobPositionName { get; init; } = string.Empty;

    public long? EducationLevelId { get; init; }

    public string? EducationLevelName { get; init; }

    public long? EvaluatorEmployeeId { get; init; }

    public string? EvaluatorFullName { get; init; }

    public long? UserId { get; init; }

    public bool IsActive { get; init; }

    public DateOnly? HiredAt { get; init; }

    public string? AvatarUrl { get; init; }
}

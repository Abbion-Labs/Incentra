namespace VariableCompensation.Api.Contracts.Hr;

public sealed class CreateOrganizationUnitRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Code { get; init; }
}

public sealed class UpdateOrganizationUnitRequest
{
    /// <summary>The version the edit was made from. Required: an edit from an outdated copy is refused.</summary>
    public int? Version { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Code { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class CreateJobPositionRequest
{
    public string Name { get; init; } = string.Empty;

    public int SortOrder { get; init; }
}

public sealed class UpdateJobPositionRequest
{
    /// <summary>The version the edit was made from. Required: an edit from an outdated copy is refused.</summary>
    public int? Version { get; init; }

    public string Name { get; init; } = string.Empty;

    public int SortOrder { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class CreateEducationLevelRequest
{
    public string Name { get; init; } = string.Empty;

    public int SortOrder { get; init; }
}

public sealed class UpdateEducationLevelRequest
{
    /// <summary>The version the edit was made from. Required: an edit from an outdated copy is refused.</summary>
    public int? Version { get; init; }

    public string Name { get; init; } = string.Empty;

    public int SortOrder { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class CreateEmployeeRequest
{
    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public long OrganizationUnitId { get; init; }

    public long JobPositionId { get; init; }

    public long? EducationLevelId { get; init; }

    public long? EvaluatorEmployeeId { get; init; }

    public DateOnly? HiredAt { get; init; }
}

public sealed class UpdateEmployeeRequest
{
    /// <summary>The version the edit was made from. Required: an edit from an outdated copy is refused.</summary>
    public int? Version { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public long OrganizationUnitId { get; init; }

    public long JobPositionId { get; init; }

    public long? EducationLevelId { get; init; }

    public long? EvaluatorEmployeeId { get; init; }

    public DateOnly? HiredAt { get; init; }

    public bool IsActive { get; init; } = true;
}

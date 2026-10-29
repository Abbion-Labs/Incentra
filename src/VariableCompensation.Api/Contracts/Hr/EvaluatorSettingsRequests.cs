namespace VariableCompensation.Api.Contracts.Hr;

public sealed class UpdateEvaluatorSettingsRequest
{
    /// <summary>The version the edit was made from. Required: an edit from an outdated copy is refused.</summary>
    public int? Version { get; init; }

    /// <summary>Null for an evaluator whose evaluations need no review.</summary>
    public long? ControllerEmployeeId { get; init; }
}

public sealed class LinkEmployeeUserRequest
{
    /// <summary>The version the edit was made from. Required: an edit from an outdated copy is refused.</summary>
    public int? Version { get; init; }

    public long? UserId { get; init; }
}

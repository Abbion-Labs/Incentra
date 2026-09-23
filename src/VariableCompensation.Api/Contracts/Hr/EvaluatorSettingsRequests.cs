namespace VariableCompensation.Api.Contracts.Hr;

public sealed class UpdateEvaluatorSettingsRequest
{
    /// <summary>Null for an evaluator whose evaluations need no review.</summary>
    public long? ControllerEmployeeId { get; init; }
}

public sealed class LinkEmployeeUserRequest
{
    public long? UserId { get; init; }
}

namespace VariableCompensation.Api.Contracts.Hr;

public sealed class UpdateEvaluatorSettingsRequest
{
    public long ControllerEmployeeId { get; init; }
}

public sealed class LinkEmployeeUserRequest
{
    public long? UserId { get; init; }
}

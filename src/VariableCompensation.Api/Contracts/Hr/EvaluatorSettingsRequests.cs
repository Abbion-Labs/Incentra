namespace VariableCompensation.Api.Contracts.Hr;

public sealed class UpdateEvaluatorSettingsRequest
{
    public long ControllerEmployeeId { get; init; }

    public decimal ThresholdDoesNotMeet { get; init; }

    public decimal ThresholdMeets { get; init; }

    public decimal ThresholdGood { get; init; }

    public decimal ThresholdExceeds { get; init; }

    public decimal PercentDoesNotMeet { get; init; }

    public decimal PercentMeets { get; init; }

    public decimal PercentGood { get; init; }

    public decimal PercentExceeds { get; init; }
}

public sealed class LinkEmployeeUserRequest
{
    public long? UserId { get; init; }
}

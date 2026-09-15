namespace VariableCompensation.Application.Compensation.Models;

public sealed class CompensationCalculationStatus
{
    public long ParametersId { get; init; }

    public int TotalResults { get; init; }

    public int FinalizedResults { get; init; }

    public bool IsFinalized { get; init; }

    public DateTime? LastCalculatedAt { get; init; }
}

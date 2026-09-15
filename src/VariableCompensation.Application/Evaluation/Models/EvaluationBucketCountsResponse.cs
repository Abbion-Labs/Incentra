namespace VariableCompensation.Application.Evaluation.Models;

public sealed class EvaluationBucketCountsResponse
{
    public int Planning { get; init; }

    public int Unrated { get; init; }

    public int Returned { get; init; }

    public int Submitted { get; init; }

    public int Approved { get; init; }

    public int Pending { get; init; }

    public int GoalsComplete { get; init; }

    public int GoalsPending { get; init; }
}

namespace VariableCompensation.Application.Abstractions.Persistence;

public interface IAnalyticsRepository
{
    Task<int> GetActiveSubordinateCountAsync(long evaluatorEmployeeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DescriptiveRatingCountRow>> GetDescriptiveRatingCountsByEvaluatorAsync(
        long evaluatorEmployeeId,
        short year,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<decimal>> GetOverallAveragesByEvaluatorAsync(
        long evaluatorEmployeeId,
        short? year,
        CancellationToken cancellationToken);
}

public sealed record DescriptiveRatingCountRow(
    long DescriptiveRatingId,
    string Code,
    string Name,
    int SortOrder,
    int Count);

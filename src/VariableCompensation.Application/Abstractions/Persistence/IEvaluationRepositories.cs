using EvaluationEntity = VariableCompensation.Domain.Entities.Evaluation.Evaluation;
using VariableCompensation.Application.Common.Models;
using VariableCompensation.Application.Evaluation.Models;
using VariableCompensation.Domain.Entities.Evaluation;
using VariableCompensation.Domain.Entities.Lookup;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Abstractions.Persistence;

public interface IEvaluationRepository
{
    Task<EvaluationEntity?> FindByIdAsync(long id, CancellationToken cancellationToken);

    Task<EvaluationEntity?> FindByIdForUpdateAsync(long id, CancellationToken cancellationToken);

    Task<IReadOnlyList<EvaluationStatusHistory>> GetStatusHistoryAsync(long evaluationId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<EvaluationEntity> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        short? year,
        byte? quarter,
        EvaluationStatus? status,
        string? bucket,
        string? search,
        long? employeeId,
        long? evaluatorEmployeeId,
        long? controllerEmployeeId,
        long? organizationUnitId,
        CancellationToken cancellationToken);

    Task<EvaluationBucketCountsResponse> GetBucketCountsAsync(
        short? year,
        byte? quarter,
        string? search,
        long? employeeId,
        long? evaluatorEmployeeId,
        long? controllerEmployeeId,
        long? organizationUnitId,
        CancellationToken cancellationToken);

    Task<bool> ExistsForEmployeeQuarterAsync(long employeeId, short year, byte quarter, long? excludeId, CancellationToken cancellationToken);

    Task<long?> GetControllerEmployeeIdAsync(long evaluatorEmployeeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<EvaluationEntity>> GetListByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ApprovedQuarterBenchmarkAverages>> GetApprovedBenchmarkAveragesByOrganizationUnitAsync(
        long organizationUnitId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ApprovedQuarterBenchmarkAverages>> GetApprovedBenchmarkAveragesByJobPositionAsync(
        long jobPositionId,
        CancellationToken cancellationToken);

    /// <summary>Whether the employee has an evaluation that is not approved yet.</summary>
    Task<bool> HasUnapprovedForEmployeeAsync(long employeeId, CancellationToken cancellationToken);

    /// <summary>Tracked evaluations of the employee in <paramref name="status"/>, without their details.</summary>
    Task<IReadOnlyList<EvaluationEntity>> GetForUpdateByEmployeeAsync(
        long employeeId,
        EvaluationStatus status,
        CancellationToken cancellationToken);

    /// <summary>Tracked evaluations rated by the evaluator that are not approved yet, without their details.</summary>
    Task<IReadOnlyList<EvaluationEntity>> GetUnapprovedForUpdateByEvaluatorAsync(
        long evaluatorEmployeeId,
        CancellationToken cancellationToken);

    Task AddAsync(EvaluationEntity entity, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IEvaluationLookupRepository
{
    Task<IReadOnlyList<RatingLevel>> GetRatingLevelsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<DescriptiveRating>> GetDescriptiveRatingsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<MeasureType>> GetMeasureTypesAsync(CancellationToken cancellationToken);

    Task<bool> RatingLevelExistsAsync(long id, CancellationToken cancellationToken);

    Task<bool> MeasureTypeExistsAsync(long id, CancellationToken cancellationToken);
}

public interface IDescriptiveRatingRepository
{
    Task<DescriptiveRating?> FindByIdAsync(long id, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DescriptiveRating>> GetAllAsync(bool? isActive, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, decimal>> GetRecommendedShareByCodeAsync(CancellationToken cancellationToken);

    Task AddAsync(DescriptiveRating entity, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

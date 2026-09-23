using VariableCompensation.Domain.Entities.Compensation;
using EvaluationEntity = VariableCompensation.Domain.Entities.Evaluation.Evaluation;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Application.Compensation.Models;

namespace VariableCompensation.Application.Abstractions.Persistence;

public interface ICompensationRepository
{
    Task<VariableCompensationParameters?> FindParametersByIdAsync(long id, CancellationToken cancellationToken);

    Task<VariableCompensationParameters?> FindParametersByIdForUpdateAsync(long id, CancellationToken cancellationToken);

    Task<IReadOnlyList<VariableCompensationParameters>> GetParametersAsync(
        long? organizationUnitId,
        short? year,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<bool> ParametersExistsForOrgUnitYearAsync(long organizationUnitId, short year, long? excludeId, CancellationToken cancellationToken);

    Task AddParametersAsync(VariableCompensationParameters entity, CancellationToken cancellationToken);

    Task<VariableCompensationResult?> FindResultByIdAsync(long id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<VariableCompensationResult> Items, int TotalCount)> GetResultsPagedAsync(
        int page,
        int pageSize,
        long? parametersId,
        short? year,
        long? organizationUnitId,
        long? employeeId,
        string? search,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<VariableCompensationResult>> GetResultsForAnalyticsAsync(
        short year,
        long? organizationUnitId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Employee>> GetActiveEmployeesByOrganizationUnitAsync(long organizationUnitId, CancellationToken cancellationToken);

    Task<IReadOnlyList<EvaluationEntity>> GetApprovedEvaluationsAsync(
        long organizationUnitId,
        short year,
        CancellationToken cancellationToken);

    Task<EvaluatorSettings?> GetEvaluatorSettingsAsync(long evaluatorEmployeeId, CancellationToken cancellationToken);

    Task<VariableCompensationResult?> FindResultForEmployeeAsync(
        long employeeId,
        long parametersId,
        short year,
        CancellationToken cancellationToken);

    Task<VariableCompensationResult?> FindResultForEmployeeForUpdateAsync(
        long employeeId,
        long parametersId,
        short year,
        CancellationToken cancellationToken);

    Task<bool> HasFinalResultsAsync(long parametersId, CancellationToken cancellationToken);

    Task<CompensationCalculationStatus> GetCalculationStatusAsync(long parametersId, CancellationToken cancellationToken);

    Task<long?> ResolveActiveParametersIdAsync(long organizationUnitId, short year, CancellationToken cancellationToken);

    Task AddResultAsync(VariableCompensationResult entity, CancellationToken cancellationToken);

    /// <summary>
    /// Removes every result of <paramref name="parametersId"/> that is not final. Staged for the next save, so a
    /// recalculation replaces the previous one as a whole.
    /// </summary>
    Task RemoveDraftResultsAsync(long parametersId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

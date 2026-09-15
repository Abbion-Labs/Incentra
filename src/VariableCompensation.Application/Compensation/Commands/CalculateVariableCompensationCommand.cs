using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Abstractions.Security;
using VariableCompensation.Application.Compensation.Models;
using VariableCompensation.Application.Compensation.Services;
using VariableCompensation.Domain;
using EvaluationEntity = VariableCompensation.Domain.Entities.Evaluation.Evaluation;
using VariableCompensation.Domain.Entities.Compensation;
using VariableCompensation.Domain.Entities.Hr;

namespace VariableCompensation.Application.Compensation.Commands;

public sealed record CalculateVariableCompensationCommand(
    long ParametersId,
    bool IsFinal,
    bool RequireAllQuarters = false,
    bool? AllowNegativeVariable = null) : IRequest<Result<CalculateCompensationResponse>>;

public sealed class CalculateVariableCompensationCommandHandler : IRequestHandler<CalculateVariableCompensationCommand, Result<CalculateCompensationResponse>>
{
    private readonly ICompensationRepository repository;
    private readonly IEmployeeSalaryRepository salaryRepository;
    private readonly ISensitiveDataEncryptionService encryptionService;
    private readonly CompensationCalculationService calculationService;
    private readonly ICurrentUserService currentUserService;

    public CalculateVariableCompensationCommandHandler(
        ICompensationRepository repository,
        IEmployeeSalaryRepository salaryRepository,
        ISensitiveDataEncryptionService encryptionService,
        CompensationCalculationService calculationService,
        ICurrentUserService currentUserService)
    {
        this.repository = repository;
        this.salaryRepository = salaryRepository;
        this.encryptionService = encryptionService;
        this.calculationService = calculationService;
        this.currentUserService = currentUserService;
    }

    public async Task<Result<CalculateCompensationResponse>> Handle(CalculateVariableCompensationCommand request, CancellationToken cancellationToken)
    {
        var parameters = await this.repository.FindParametersByIdAsync(request.ParametersId, cancellationToken);
        if (parameters is null || !parameters.IsActive)
        {
            return Result.Failure<CalculateCompensationResponse>(ErrorCodes.CompensationParametersInactiveOrMissing);
        }

        if (await this.repository.HasFinalResultsAsync(request.ParametersId, cancellationToken))
        {
            return Result.Failure<CalculateCompensationResponse>(ErrorCodes.FinalizedResultsAlreadyExist);
        }

        var employees = await this.repository.GetActiveEmployeesByOrganizationUnitAsync(parameters.OrganizationUnitId, cancellationToken);
        var employeeIds = employees.Select(e => e.Id).ToList();
        var asOfDate = new DateOnly(parameters.Year, 12, 31);
        var salaryRows = await this.salaryRepository.GetEffectiveByEmployeeIdsAsync(employeeIds, asOfDate, cancellationToken);
        var approvedEvaluations = await this.repository.GetApprovedEvaluationsAsync(parameters.OrganizationUnitId, parameters.Year, cancellationToken);
        var evaluationsByEmployee = approvedEvaluations.GroupBy(e => e.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        var allowNegativeVariable = request.AllowNegativeVariable ?? parameters.AllowNegativeVariable;
        var warnings = new List<string>();
        var inputs = new List<CompensationCalculationService.EmployeeCalculationInput>();

        foreach (var employee in employees)
        {
            if (!evaluationsByEmployee.TryGetValue(employee.Id, out var employeeEvaluations))
            {
                employeeEvaluations = new List<EvaluationEntity>();
            }

            if (!salaryRows.TryGetValue(employee.Id, out var salaryRow))
            {
                warnings.Add($"Employee {employee.FullName} has no configured points and salary per point.");
                continue;
            }

            var salaryPerPoint = this.encryptionService.DecryptDecimal(salaryRow.EncryptedSalaryPerPoint);

            var (input, skipReason) = this.calculationService.BuildEmployeeInput(
                employee,
                employeeEvaluations,
                parameters,
                request.RequireAllQuarters,
                salaryRow.Points,
                salaryPerPoint,
                allowNegativeVariable);

            if (input is null)
            {
                warnings.Add(skipReason!);
                continue;
            }

            inputs.Add(input);
        }

        if (inputs.Count == 0)
        {
            return Result.Failure<CalculateCompensationResponse>(ErrorCodes.NoEmployeesEligible);
        }

        var outputs = this.calculationService.DistributePool(inputs, parameters);

        foreach (var output in outputs)
        {
            var existing = await this.repository.FindResultForEmployeeForUpdateAsync(
                output.Input.Employee.Id,
                parameters.Id,
                parameters.Year,
                cancellationToken);

            if (existing is not null)
            {
                if (existing.IsFinal)
                {
                    warnings.Add($"Skipping finalized result for {output.Input.Employee.FullName}.");
                    continue;
                }

                await this.repository.RemoveResultAsync(existing, cancellationToken);
            }

            var result = new VariableCompensationResult
            {
                EmployeeId = output.Input.Employee.Id,
                ParametersId = parameters.Id,
                Year = parameters.Year,
                GoalsAverage = output.Input.GoalsAverage,
                MeasuresAverage = output.Input.MeasuresAverage,
                OverallAverage = output.Input.OverallAverage,
                Points = output.Input.Points,
                SalaryPointsValue = output.Input.SalaryPerPoint,
                Weight = output.Input.Ponder,
                ZScore = output.ZScore,
                NormalizedZScore = output.NormalizedZScore,
                NormalizedPoints = output.NormalizedPoints,
                CompensationWithoutSalary = output.CompensationWithoutSalary,
                Aq = output.Aq,
                NetCompensation = output.NetCompensation,
                QuarterlyCompensation = output.QuarterlyCompensation,
                MonthlyCompensation = output.MonthlyCompensation,
                CompensationPercent = output.CompensationPercent,
                FormulaVersion = CompensationCalculationService.FormulaVersion,
                CalculatedAt = DateTime.UtcNow,
                IsFinal = request.IsFinal,
                CreatedByUserId = this.currentUserService.UserId,
                EvaluationLinks = output.Input.Evaluations.Select(e => new VariableCompensationEvaluationLink
                {
                    EvaluationId = e.Id
                }).ToList()
            };

            await this.repository.AddResultAsync(result, cancellationToken);
        }

        await this.repository.SaveChangesAsync(cancellationToken);

        var reloaded = await this.repository.GetResultsPagedAsync(1, 100, parameters.Id, parameters.Year, parameters.OrganizationUnitId, null, null, cancellationToken);
        return Result.Success(new CalculateCompensationResponse
        {
            ParametersId = parameters.Id,
            EmployeesCalculated = outputs.Count,
            EmployeesSkipped = employees.Count - outputs.Count,
            Warnings = warnings,
            Results = reloaded.Items.Select(CompensationMappings.ToSummary).ToList()
        });
    }
}

public sealed record FinalizeCompensationResultsCommand(long ParametersId) : IRequest<Result<int>>;

public sealed class FinalizeCompensationResultsCommandHandler : IRequestHandler<FinalizeCompensationResultsCommand, Result<int>>
{
    private readonly ICompensationRepository repository;

    public FinalizeCompensationResultsCommandHandler(ICompensationRepository repository) => this.repository = repository;

    public async Task<Result<int>> Handle(FinalizeCompensationResultsCommand request, CancellationToken cancellationToken)
    {
        var parameters = await this.repository.FindParametersByIdAsync(request.ParametersId, cancellationToken);
        if (parameters is null)
        {
            return Result.Failure<int>(ErrorCodes.CompensationParametersNotFound);
        }

        var (results, _) = await this.repository.GetResultsPagedAsync(1, 1000, request.ParametersId, parameters.Year, null, null, null, cancellationToken);
        if (results.Count == 0)
        {
            return Result.Failure<int>(ErrorCodes.NoResultsToFinalize);
        }

        var count = 0;
        foreach (var summary in results)
        {
            var entity = await this.repository.FindResultForEmployeeForUpdateAsync(summary.EmployeeId, request.ParametersId, parameters.Year, cancellationToken);
            if (entity is null || entity.IsFinal)
            {
                continue;
            }

            entity.IsFinal = true;
            count++;
        }

        await this.repository.SaveChangesAsync(cancellationToken);
        return Result.Success(count);
    }
}

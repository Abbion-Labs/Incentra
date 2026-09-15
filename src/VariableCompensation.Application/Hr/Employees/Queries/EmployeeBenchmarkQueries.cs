using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Application.Hr.Models;
using VariableCompensation.Application.Hr.Services;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Hr.Employees.Queries;

public sealed record GetEmployeeEvaluationBenchmarksQuery(long EmployeeId) : IRequest<Result<EmployeeEvaluationBenchmarksResponse>>;

public sealed class GetEmployeeEvaluationBenchmarksQueryHandler
    : IRequestHandler<GetEmployeeEvaluationBenchmarksQuery, Result<EmployeeEvaluationBenchmarksResponse>>
{
    private readonly IEmployeeRepository employeeRepository;
    private readonly IEvaluationRepository evaluationRepository;
    private readonly ICurrentUserService currentUserService;
    private readonly ICurrentEmployeeContext currentEmployeeContext;
    private readonly ControllerSupervisionService controllerSupervisionService;

    public GetEmployeeEvaluationBenchmarksQueryHandler(
        IEmployeeRepository employeeRepository,
        IEvaluationRepository evaluationRepository,
        ICurrentUserService currentUserService,
        ICurrentEmployeeContext currentEmployeeContext,
        ControllerSupervisionService controllerSupervisionService)
    {
        this.employeeRepository = employeeRepository;
        this.evaluationRepository = evaluationRepository;
        this.currentUserService = currentUserService;
        this.currentEmployeeContext = currentEmployeeContext;
        this.controllerSupervisionService = controllerSupervisionService;
    }

    public async Task<Result<EmployeeEvaluationBenchmarksResponse>> Handle(
        GetEmployeeEvaluationBenchmarksQuery request,
        CancellationToken cancellationToken)
    {
        var employee = await this.employeeRepository.FindByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<EmployeeEvaluationBenchmarksResponse>(ErrorCodes.EmployeeNotFound);
        }

        var access = await this.EnsureCanViewEmployeeAsync(employee, cancellationToken);
        if (access.IsFailure)
        {
            return Result.Failure<EmployeeEvaluationBenchmarksResponse>(access.Error);
        }

        var evaluations = await this.evaluationRepository.GetListByEmployeeIdAsync(request.EmployeeId, cancellationToken);
        var orgAverages = await this.evaluationRepository.GetApprovedBenchmarkAveragesByOrganizationUnitAsync(
            employee.OrganizationUnitId,
            cancellationToken);
        var jobAverages = await this.evaluationRepository.GetApprovedBenchmarkAveragesByJobPositionAsync(
            employee.JobPositionId,
            cancellationToken);

        var orgLookup = orgAverages.ToDictionary(x => (x.Year, x.Quarter));
        var jobLookup = jobAverages.ToDictionary(x => (x.Year, x.Quarter));

        var quarters = evaluations.Select(e =>
        {
            var key = (e.Year, e.Quarter);
            var hasIncomplete = e.ConditionsFulfilled && (
                e.Goals.Any(g => g.RatingLevel?.Value == RatingLevelRules.NotRatedValue)
                || e.Measures.Any(m => m.RatingLevel?.Value == RatingLevelRules.NotRatedValue)
                || (e.Goals.Count > 0 && e.Measures.Count == 0));
            orgLookup.TryGetValue(key, out var orgBenchmark);
            jobLookup.TryGetValue(key, out var jobBenchmark);
            return new EmployeeQuarterBenchmarkResponse
            {
                EvaluationId = e.Id,
                Year = e.Year,
                Quarter = e.Quarter,
                Status = e.Status.ToString(),
                EmployeeAverage = hasIncomplete ? null : e.OverallAverage,
                EmployeeGoalsAverage = hasIncomplete ? null : e.GoalsAverage,
                EmployeeMeasuresAverage = hasIncomplete ? null : e.MeasuresAverage,
                OrganizationUnitAverage = orgBenchmark?.OverallAverage,
                OrganizationUnitGoalsAverage = orgBenchmark?.GoalsAverage,
                OrganizationUnitMeasuresAverage = orgBenchmark?.MeasuresAverage,
                JobPositionAverage = jobBenchmark?.OverallAverage,
                JobPositionGoalsAverage = jobBenchmark?.GoalsAverage,
                JobPositionMeasuresAverage = jobBenchmark?.MeasuresAverage,
                HasIncompleteRatings = hasIncomplete,
                DescriptiveRatingName = e.DescriptiveRating?.Name,
            };
        }).ToList();

        return new EmployeeEvaluationBenchmarksResponse
        {
            Employee = HrMappings.ToResponse(employee),
            Quarters = quarters,
        };
    }

    private async Task<Result> EnsureCanViewEmployeeAsync(
        Domain.Entities.Hr.Employee employee,
        CancellationToken cancellationToken)
    {
        if (this.currentUserService.IsAdmin)
        {
            return Result.Success();
        }

        var currentEmployeeId = await this.currentEmployeeContext.GetEmployeeIdAsync(cancellationToken);
        if (currentEmployeeId is null)
        {
            return Result.Failure(ErrorCodes.UserNotLinkedToEmployee);
        }

        if (employee.Id == currentEmployeeId)
        {
            return Result.Success();
        }

        if (this.currentUserService.IsInRole(RoleCodes.Evaluator) &&
            employee.EvaluatorEmployeeId == currentEmployeeId)
        {
            return Result.Success();
        }

        if (this.currentUserService.IsInRole(RoleCodes.Controller) &&
            await this.controllerSupervisionService.SupervisesEmployeeAsync(currentEmployeeId.Value, employee.Id, cancellationToken))
        {
            return Result.Success();
        }

        return Result.Failure(ErrorCodes.EmployeeAccessDenied);
    }
}

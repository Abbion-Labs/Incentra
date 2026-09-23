using CSharpFunctionalExtensions;
using EvaluationEntity = VariableCompensation.Domain.Entities.Evaluation.Evaluation;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Evaluation.Services;

public sealed class EvaluationAccessService
{
    private readonly ICurrentUserService currentUserService;
    private readonly ICurrentEmployeeContext currentEmployeeContext;

    public EvaluationAccessService(ICurrentUserService currentUserService, ICurrentEmployeeContext currentEmployeeContext)
    {
        this.currentUserService = currentUserService;
        this.currentEmployeeContext = currentEmployeeContext;
    }

    /// <summary>
    /// Narrows an evaluation list to what the user may see in the role their
    /// session works in. The token carries only that role.
    /// </summary>
    public async Task<(long? EmployeeId, long? EvaluatorEmployeeId, long? ControllerEmployeeId)> ResolveListFiltersAsync(
        long? employeeId,
        long? evaluatorEmployeeId,
        long? controllerEmployeeId,
        CancellationToken cancellationToken)
    {
        if (this.currentUserService.IsAdmin)
        {
            return (employeeId, evaluatorEmployeeId, controllerEmployeeId);
        }

        var currentEmployeeId = await this.currentEmployeeContext.GetEmployeeIdAsync(cancellationToken);
        if (currentEmployeeId is null)
        {
            return (-1, -1, -1);
        }

        return this.currentUserService.ActiveRole switch
        {
            RoleCodes.Employee => (currentEmployeeId, null, null),
            RoleCodes.Evaluator => (employeeId, currentEmployeeId, null),
            RoleCodes.Controller => (employeeId, evaluatorEmployeeId, currentEmployeeId),
            _ => (-1, -1, -1),
        };
    }

    public async Task<Result> EnsureCanViewAsync(EvaluationEntity evaluation, CancellationToken cancellationToken)
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

        if (evaluation.EmployeeId == currentEmployeeId ||
            evaluation.EvaluatorEmployeeId == currentEmployeeId ||
            evaluation.ControllerEmployeeId == currentEmployeeId)
        {
            return Result.Success();
        }

        return Result.Failure(ErrorCodes.EvaluationAccessDenied);
    }

    public async Task<Result> EnsureCanEditDraftAsync(EvaluationEntity evaluation, CancellationToken cancellationToken)
    {
        if (this.currentUserService.IsAdmin)
        {
            return Result.Success();
        }

        if (!this.currentUserService.IsInRole(RoleCodes.Evaluator))
        {
            return Result.Failure(ErrorCodes.EvaluatorOnlyEditDraft);
        }

        var currentEmployeeId = await this.currentEmployeeContext.GetEmployeeIdAsync(cancellationToken);
        if (currentEmployeeId is null)
        {
            return Result.Failure(ErrorCodes.UserNotLinkedToEmployee);
        }

        if (evaluation.EvaluatorEmployeeId != currentEmployeeId)
        {
            return Result.Failure(ErrorCodes.EvaluatorOnlyOwnAssigned);
        }

        return Result.Success();
    }

    public Task<Result> EnsureCanSubmitAsync(EvaluationEntity evaluation, CancellationToken cancellationToken) =>
        this.EnsureCanEditDraftAsync(evaluation, cancellationToken);

    public async Task<Result> EnsureCanReviewAsync(EvaluationEntity evaluation, CancellationToken cancellationToken)
    {
        if (this.currentUserService.IsAdmin)
        {
            return Result.Success();
        }

        if (!this.currentUserService.IsInRole(RoleCodes.Controller))
        {
            return Result.Failure(ErrorCodes.ControllerOnlyReview);
        }

        var currentEmployeeId = await this.currentEmployeeContext.GetEmployeeIdAsync(cancellationToken);
        if (currentEmployeeId is null)
        {
            return Result.Failure(ErrorCodes.UserNotLinkedToEmployee);
        }

        if (evaluation.ControllerEmployeeId != currentEmployeeId)
        {
            return Result.Failure(ErrorCodes.ControllerOnlyOwnAssigned);
        }

        return Result.Success();
    }

    public async Task<Result> EnsureCanCreateForEmployeeAsync(long employeeEvaluatorId, CancellationToken cancellationToken)
    {
        if (this.currentUserService.IsAdmin)
        {
            return Result.Success();
        }

        if (!this.currentUserService.IsInRole(RoleCodes.Evaluator))
        {
            return Result.Failure(ErrorCodes.EvaluatorOnlyCreate);
        }

        var currentEmployeeId = await this.currentEmployeeContext.GetEmployeeIdAsync(cancellationToken);
        if (currentEmployeeId is null)
        {
            return Result.Failure(ErrorCodes.UserNotLinkedToEmployee);
        }

        if (employeeEvaluatorId != currentEmployeeId)
        {
            return Result.Failure(ErrorCodes.EvaluatorOnlyCreateAssigned);
        }

        return Result.Success();
    }
}

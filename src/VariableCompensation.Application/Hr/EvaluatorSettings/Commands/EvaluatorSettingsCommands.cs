using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Hr.Models;
using EvaluatorSettingsEntity = VariableCompensation.Domain.Entities.Hr.EvaluatorSettings;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Hr;

namespace VariableCompensation.Application.Hr.EvaluatorSettings.Commands;

/// <summary>
/// Evaluator settings are created by granting the EVALUATOR role, never on
/// their own: a settings row without the role would be an evaluator who
/// cannot sign in and rate anyone, which is the drift this design removes.
/// Only the thresholds and the controller can be changed here.
/// </summary>
internal static class EvaluatorSettingsValidation
{
    internal static Result ThresholdsAndPercents(
        decimal thresholdDoesNotMeet,
        decimal thresholdMeets,
        decimal thresholdGood,
        decimal thresholdExceeds,
        decimal percentDoesNotMeet,
        decimal percentMeets,
        decimal percentGood,
        decimal percentExceeds)
    {
        if (thresholdDoesNotMeet >= thresholdMeets ||
            thresholdMeets >= thresholdGood ||
            thresholdGood >= thresholdExceeds)
        {
            return Result.Failure(ErrorCodes.ThresholdOrderInvalid);
        }

        if (percentDoesNotMeet < 0 || percentMeets < 0 ||
            percentGood < 0 || percentExceeds < 0)
        {
            return Result.Failure(ErrorCodes.PercentNegative);
        }

        return Result.Success();
    }
}

public sealed record UpdateEvaluatorSettingsCommand(
    long EmployeeId,
    long ControllerEmployeeId,
    decimal ThresholdDoesNotMeet,
    decimal ThresholdMeets,
    decimal ThresholdGood,
    decimal ThresholdExceeds,
    decimal PercentDoesNotMeet,
    decimal PercentMeets,
    decimal PercentGood,
    decimal PercentExceeds) : IRequest<Result<EvaluatorSettingsResponse>>;

public sealed class UpdateEvaluatorSettingsCommandHandler : IRequestHandler<UpdateEvaluatorSettingsCommand, Result<EvaluatorSettingsResponse>>
{
    private readonly IEvaluatorSettingsRepository repository;
    private readonly IEmployeeRepository employeeRepository;
    private readonly IUserRepository userRepository;

    public UpdateEvaluatorSettingsCommandHandler(
        IEvaluatorSettingsRepository repository,
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository)
    {
        this.repository = repository;
        this.employeeRepository = employeeRepository;
        this.userRepository = userRepository;
    }

    public async Task<Result<EvaluatorSettingsResponse>> Handle(UpdateEvaluatorSettingsCommand request, CancellationToken cancellationToken)
    {
        var validation = EvaluatorSettingsValidation.ThresholdsAndPercents(
            request.ThresholdDoesNotMeet,
            request.ThresholdMeets,
            request.ThresholdGood,
            request.ThresholdExceeds,
            request.PercentDoesNotMeet,
            request.PercentMeets,
            request.PercentGood,
            request.PercentExceeds);

        if (validation.IsFailure)
        {
            return Result.Failure<EvaluatorSettingsResponse>(validation.Error);
        }

        var entity = await this.repository.FindByEmployeeIdForUpdateAsync(request.EmployeeId, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<EvaluatorSettingsResponse>(ErrorCodes.EvaluatorSettingsNotFound);
        }

        var controllerCheck = await Services.ControllerRoleCheck.EnsureIsAControllerAsync(
            request.ControllerEmployeeId,
            this.employeeRepository,
            this.userRepository,
            cancellationToken);
        if (controllerCheck.IsFailure)
        {
            return Result.Failure<EvaluatorSettingsResponse>(controllerCheck.Error);
        }

        entity.ControllerEmployeeId = request.ControllerEmployeeId;
        entity.ThresholdDoesNotMeet = request.ThresholdDoesNotMeet;
        entity.ThresholdMeets = request.ThresholdMeets;
        entity.ThresholdGood = request.ThresholdGood;
        entity.ThresholdExceeds = request.ThresholdExceeds;
        entity.PercentDoesNotMeet = request.PercentDoesNotMeet;
        entity.PercentMeets = request.PercentMeets;
        entity.PercentGood = request.PercentGood;
        entity.PercentExceeds = request.PercentExceeds;
        entity.UpdatedAt = DateTime.UtcNow;

        await this.repository.SaveChangesAsync(cancellationToken);

        var updated = await this.repository.FindByEmployeeIdAsync(request.EmployeeId, cancellationToken);
        return Result.Success(HrMappings.ToResponse(updated!));
    }
}

public sealed record LinkEmployeeUserCommand(long EmployeeId, long? UserId) : IRequest<Result<EmployeeResponse>>;

public sealed class LinkEmployeeUserCommandHandler : IRequestHandler<LinkEmployeeUserCommand, Result<EmployeeResponse>>
{
    private readonly IEmployeeRepository employeeRepository;
    private readonly IUserRepository userRepository;
    private readonly ICurrentUserService currentUserService;

    public LinkEmployeeUserCommandHandler(
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        this.employeeRepository = employeeRepository;
        this.userRepository = userRepository;
        this.currentUserService = currentUserService;
    }

    public async Task<Result<EmployeeResponse>> Handle(LinkEmployeeUserCommand request, CancellationToken cancellationToken)
    {
        var entity = await this.employeeRepository.FindByIdForUpdateAsync(request.EmployeeId, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<EmployeeResponse>(ErrorCodes.EmployeeNotFound);
        }

        if (request.UserId is not null)
        {
            var user = await this.userRepository.FindByIdWithRolesAsync(request.UserId.Value, cancellationToken);
            if (user is null || !user.IsActive)
            {
                return Result.Failure<EmployeeResponse>(ErrorCodes.UserNotFound);
            }

            if (await this.employeeRepository.IsUserLinkedToAnotherEmployeeAsync(request.UserId.Value, request.EmployeeId, cancellationToken))
            {
                return Result.Failure<EmployeeResponse>(ErrorCodes.UserAlreadyLinked);
            }
        }

        entity.UserId = request.UserId;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = this.currentUserService.UserId;

        await this.employeeRepository.SaveChangesAsync(cancellationToken);

        var updated = await this.employeeRepository.FindByIdAsync(request.EmployeeId, cancellationToken);
        return Result.Success(HrMappings.ToResponse(updated!));
    }
}

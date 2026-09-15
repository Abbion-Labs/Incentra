using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Hr.Models;
using EvaluatorSettingsEntity = VariableCompensation.Domain.Entities.Hr.EvaluatorSettings;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Hr;

namespace VariableCompensation.Application.Hr.EvaluatorSettings.Commands;

public sealed record CreateEvaluatorSettingsCommand(
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

public sealed class CreateEvaluatorSettingsCommandHandler : IRequestHandler<CreateEvaluatorSettingsCommand, Result<EvaluatorSettingsResponse>>
{
    private readonly IEvaluatorSettingsRepository repository;
    private readonly IEmployeeRepository employeeRepository;

    public CreateEvaluatorSettingsCommandHandler(IEvaluatorSettingsRepository repository, IEmployeeRepository employeeRepository)
    {
        this.repository = repository;
        this.employeeRepository = employeeRepository;
    }

    public async Task<Result<EvaluatorSettingsResponse>> Handle(CreateEvaluatorSettingsCommand request, CancellationToken cancellationToken)
    {
        var validation = ValidateThresholdsAndPercents(request);
        if (validation.IsFailure)
        {
            return Result.Failure<EvaluatorSettingsResponse>(validation.Error);
        }

        if (!await this.employeeRepository.ExistsAsync(request.EmployeeId, cancellationToken))
        {
            return Result.Failure<EvaluatorSettingsResponse>(ErrorCodes.EvaluatorNotFound);
        }

        if (!await this.employeeRepository.ExistsAsync(request.ControllerEmployeeId, cancellationToken))
        {
            return Result.Failure<EvaluatorSettingsResponse>(ErrorCodes.ControllerNotFound);
        }

        if (await this.repository.ExistsAsync(request.EmployeeId, cancellationToken))
        {
            return Result.Failure<EvaluatorSettingsResponse>(ErrorCodes.EvaluatorSettingsExists);
        }

        var entity = new EvaluatorSettingsEntity
        {
            EmployeeId = request.EmployeeId,
            ControllerEmployeeId = request.ControllerEmployeeId,
            ThresholdDoesNotMeet = request.ThresholdDoesNotMeet,
            ThresholdMeets = request.ThresholdMeets,
            ThresholdGood = request.ThresholdGood,
            ThresholdExceeds = request.ThresholdExceeds,
            PercentDoesNotMeet = request.PercentDoesNotMeet,
            PercentMeets = request.PercentMeets,
            PercentGood = request.PercentGood,
            PercentExceeds = request.PercentExceeds
        };

        await this.repository.AddAsync(entity, cancellationToken);
        await this.repository.SaveChangesAsync(cancellationToken);

        var created = await this.repository.FindByEmployeeIdAsync(request.EmployeeId, cancellationToken);
        return Result.Success(HrMappings.ToResponse(created!));
    }

    internal static Result ValidateThresholdsAndPercents(CreateEvaluatorSettingsCommand request)
    {
        if (request.ThresholdDoesNotMeet >= request.ThresholdMeets ||
            request.ThresholdMeets >= request.ThresholdGood ||
            request.ThresholdGood >= request.ThresholdExceeds)
        {
            return Result.Failure(ErrorCodes.ThresholdOrderInvalid);
        }

        if (request.PercentDoesNotMeet < 0 || request.PercentMeets < 0 ||
            request.PercentGood < 0 || request.PercentExceeds < 0)
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

    public UpdateEvaluatorSettingsCommandHandler(IEvaluatorSettingsRepository repository, IEmployeeRepository employeeRepository)
    {
        this.repository = repository;
        this.employeeRepository = employeeRepository;
    }

    public async Task<Result<EvaluatorSettingsResponse>> Handle(UpdateEvaluatorSettingsCommand request, CancellationToken cancellationToken)
    {
        var validation = CreateEvaluatorSettingsCommandHandler.ValidateThresholdsAndPercents(
            new CreateEvaluatorSettingsCommand(
                request.EmployeeId,
                request.ControllerEmployeeId,
                request.ThresholdDoesNotMeet,
                request.ThresholdMeets,
                request.ThresholdGood,
                request.ThresholdExceeds,
                request.PercentDoesNotMeet,
                request.PercentMeets,
                request.PercentGood,
                request.PercentExceeds));

        if (validation.IsFailure)
        {
            return Result.Failure<EvaluatorSettingsResponse>(validation.Error);
        }

        var entity = await this.repository.FindByEmployeeIdForUpdateAsync(request.EmployeeId, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<EvaluatorSettingsResponse>(ErrorCodes.EvaluatorSettingsNotFound);
        }

        if (!await this.employeeRepository.ExistsAsync(request.ControllerEmployeeId, cancellationToken))
        {
            return Result.Failure<EvaluatorSettingsResponse>(ErrorCodes.ControllerNotFound);
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

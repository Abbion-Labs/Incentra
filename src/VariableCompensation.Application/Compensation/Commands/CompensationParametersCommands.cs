using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Compensation.Models;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Compensation;

namespace VariableCompensation.Application.Compensation.Commands;

public sealed record CreateCompensationParametersCommand(
    long OrganizationUnitId,
    short Year,
    decimal MonetaryPool,
    string Currency,
    decimal AcceptablePerformanceRating,
    decimal DependencyWeight,
    decimal Exponent,
    bool AllowNegativeVariable) : IRequest<Result<CompensationParametersResponse>>;

public sealed class CreateCompensationParametersCommandHandler : IRequestHandler<CreateCompensationParametersCommand, Result<CompensationParametersResponse>>
{
    private readonly ICompensationRepository repository;
    private readonly IOrganizationUnitRepository organizationUnitRepository;
    private readonly ICurrentUserService currentUserService;

    public CreateCompensationParametersCommandHandler(
        ICompensationRepository repository,
        IOrganizationUnitRepository organizationUnitRepository,
        ICurrentUserService currentUserService)
    {
        this.repository = repository;
        this.organizationUnitRepository = organizationUnitRepository;
        this.currentUserService = currentUserService;
    }

    public async Task<Result<CompensationParametersResponse>> Handle(CreateCompensationParametersCommand request, CancellationToken cancellationToken)
    {
        var validation = ValidateParameters(
            request.MonetaryPool,
            request.Currency,
            request.AcceptablePerformanceRating,
            request.DependencyWeight,
            request.Exponent);
        if (validation.IsFailure)
        {
            return Result.Failure<CompensationParametersResponse>(validation.Error);
        }

        if (await this.organizationUnitRepository.FindByIdAsync(request.OrganizationUnitId, cancellationToken) is null)
        {
            return Result.Failure<CompensationParametersResponse>(ErrorCodes.OrganizationUnitNotFound);
        }

        if (await this.repository.ParametersExistsForOrgUnitYearAsync(request.OrganizationUnitId, request.Year, null, cancellationToken))
        {
            return Result.Failure<CompensationParametersResponse>(ErrorCodes.CompensationParametersExistForYear);
        }

        var entity = new VariableCompensationParameters
        {
            OrganizationUnitId = request.OrganizationUnitId,
            Year = request.Year,
            MonetaryPool = request.MonetaryPool,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            AcceptablePerformanceRating = request.AcceptablePerformanceRating,
            DependencyWeight = request.DependencyWeight,
            Exponent = request.Exponent,
            AllowNegativeVariable = request.AllowNegativeVariable,
            IsActive = true,
            CreatedByUserId = this.currentUserService.UserId
        };

        await this.repository.AddParametersAsync(entity, cancellationToken);
        await this.repository.SaveChangesAsync(cancellationToken);

        var created = await this.repository.FindParametersByIdAsync(entity.Id, cancellationToken);
        return Result.Success(CompensationMappings.ToResponse(created!));
    }

    internal static Result ValidateParameters(
        decimal monetaryPool,
        string currency,
        decimal acceptablePerformanceRating,
        decimal dependencyWeight,
        decimal exponent)
    {
        if (monetaryPool <= 0)
        {
            return Result.Failure(ErrorCodes.MonetaryPoolInvalid);
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            return Result.Failure(ErrorCodes.CurrencyInvalid);
        }

        if (acceptablePerformanceRating < 1 || acceptablePerformanceRating > 5)
        {
            return Result.Failure(ErrorCodes.AcceptablePerformanceRatingInvalid);
        }

        if (dependencyWeight <= 0)
        {
            return Result.Failure(ErrorCodes.DependencyWeightInvalid);
        }

        if (exponent <= 0)
        {
            return Result.Failure(ErrorCodes.ExponentInvalid);
        }

        return Result.Success();
    }
}

public sealed record UpdateCompensationParametersCommand(
    long Id,
    decimal MonetaryPool,
    string Currency,
    decimal AcceptablePerformanceRating,
    decimal DependencyWeight,
    decimal Exponent,
    bool AllowNegativeVariable,
    bool IsActive) : IRequest<Result<CompensationParametersResponse>>;

public sealed class UpdateCompensationParametersCommandHandler : IRequestHandler<UpdateCompensationParametersCommand, Result<CompensationParametersResponse>>
{
    private readonly ICompensationRepository repository;

    public UpdateCompensationParametersCommandHandler(ICompensationRepository repository) => this.repository = repository;

    public async Task<Result<CompensationParametersResponse>> Handle(UpdateCompensationParametersCommand request, CancellationToken cancellationToken)
    {
        var validation = CreateCompensationParametersCommandHandler.ValidateParameters(
            request.MonetaryPool,
            request.Currency,
            request.AcceptablePerformanceRating,
            request.DependencyWeight,
            request.Exponent);
        if (validation.IsFailure)
        {
            return Result.Failure<CompensationParametersResponse>(validation.Error);
        }

        var entity = await this.repository.FindParametersByIdForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<CompensationParametersResponse>(ErrorCodes.CompensationParametersNotFound);
        }

        if (await this.repository.HasFinalResultsAsync(request.Id, cancellationToken))
        {
            return Result.Failure<CompensationParametersResponse>(ErrorCodes.UpdateFinalizedParametersForbidden);
        }

        entity.MonetaryPool = request.MonetaryPool;
        entity.Currency = request.Currency.Trim().ToUpperInvariant();
        entity.AcceptablePerformanceRating = request.AcceptablePerformanceRating;
        entity.DependencyWeight = request.DependencyWeight;
        entity.Exponent = request.Exponent;
        entity.AllowNegativeVariable = request.AllowNegativeVariable;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await this.repository.SaveChangesAsync(cancellationToken);

        var updated = await this.repository.FindParametersByIdAsync(entity.Id, cancellationToken);
        return Result.Success(CompensationMappings.ToResponse(updated!));
    }
}

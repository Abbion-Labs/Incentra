using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Common;
using VariableCompensation.Application.Evaluation.Models;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Lookup;
using static VariableCompensation.Application.Evaluation.EvaluationMappings;

namespace VariableCompensation.Application.Evaluation.DescriptiveRatings.Commands;

public sealed record CreateDescriptiveRatingCommand(
    string Code,
    string Name,
    decimal MinAverage,
    decimal MaxAverage,
    int SortOrder,
    decimal RecommendedShare) : IRequest<Result<DescriptiveRatingResponse>>;

public sealed record UpdateDescriptiveRatingCommand(
    long Id,
    string Code,
    string Name,
    decimal MinAverage,
    decimal MaxAverage,
    int SortOrder,
    decimal RecommendedShare,
    bool IsActive,
    int? Version) : IRequest<Result<DescriptiveRatingResponse>>;

public sealed class CreateDescriptiveRatingCommandHandler
    : IRequestHandler<CreateDescriptiveRatingCommand, Result<DescriptiveRatingResponse>>
{
    private readonly IDescriptiveRatingRepository repository;

    public CreateDescriptiveRatingCommandHandler(IDescriptiveRatingRepository repository) =>
        this.repository = repository;

    public async Task<Result<DescriptiveRatingResponse>> Handle(
        CreateDescriptiveRatingCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await DescriptiveRatingValidation.ValidateAsync(
            this.repository,
            request.Code,
            request.Name,
            request.MinAverage,
            request.MaxAverage,
            request.RecommendedShare,
            null,
            cancellationToken);

        if (validation.IsFailure)
        {
            return Result.Failure<DescriptiveRatingResponse>(validation.Error);
        }

        var entity = new DescriptiveRating
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            MinAverage = request.MinAverage,
            MaxAverage = request.MaxAverage,
            SortOrder = request.SortOrder,
            RecommendedShare = request.RecommendedShare,
            IsActive = true,
        };

        await this.repository.AddAsync(entity, cancellationToken);
        await this.repository.SaveChangesAsync(cancellationToken);
        return Result.Success(ToDescriptiveRating(entity));
    }
}

public sealed class UpdateDescriptiveRatingCommandHandler
    : IRequestHandler<UpdateDescriptiveRatingCommand, Result<DescriptiveRatingResponse>>
{
    private readonly IDescriptiveRatingRepository repository;

    public UpdateDescriptiveRatingCommandHandler(IDescriptiveRatingRepository repository) =>
        this.repository = repository;

    public async Task<Result<DescriptiveRatingResponse>> Handle(
        UpdateDescriptiveRatingCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await this.repository.FindByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<DescriptiveRatingResponse>(ErrorCodes.DescriptiveRatingNotFound);
        }

        var version = EditVersion.Claim(entity, request.Version);
        if (version.IsFailure)
        {
            return Result.Failure<DescriptiveRatingResponse>(version.Error);
        }

        var validation = await DescriptiveRatingValidation.ValidateAsync(
            this.repository,
            request.Code,
            request.Name,
            request.MinAverage,
            request.MaxAverage,
            request.RecommendedShare,
            request.Id,
            cancellationToken);

        if (validation.IsFailure)
        {
            return Result.Failure<DescriptiveRatingResponse>(validation.Error);
        }

        entity.Code = request.Code.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();
        entity.MinAverage = request.MinAverage;
        entity.MaxAverage = request.MaxAverage;
        entity.SortOrder = request.SortOrder;
        entity.RecommendedShare = request.RecommendedShare;
        entity.IsActive = request.IsActive;

        await this.repository.SaveChangesAsync(cancellationToken);
        return Result.Success(ToDescriptiveRating(entity));
    }
}

internal static class DescriptiveRatingValidation
{
    internal static async Task<Result> ValidateAsync(
        IDescriptiveRatingRepository repository,
        string code,
        string name,
        decimal minAverage,
        decimal maxAverage,
        decimal recommendedShare,
        long? excludeId,
        CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var normalizedName = name.Trim();

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return Result.Failure(ErrorCodes.CodeRequired);
        }

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return Result.Failure(ErrorCodes.NameRequired);
        }

        if (minAverage > maxAverage)
        {
            return Result.Failure(ErrorCodes.MinAverageGreaterThanMax);
        }

        if (recommendedShare < 0 || recommendedShare > 1)
        {
            return Result.Failure(ErrorCodes.RecommendedShareInvalid);
        }

        if (await repository.CodeExistsAsync(normalizedCode, excludeId, cancellationToken))
        {
            return Result.Failure(ErrorCodes.DescriptiveRatingCodeExists);
        }

        var all = await repository.GetAllAsync(null, cancellationToken);
        foreach (var other in all.Where(x => x.IsActive && x.Id != excludeId))
        {
            if (minAverage <= other.MaxAverage && maxAverage >= other.MinAverage)
            {
                return Result.Failure($"{ErrorCodes.AverageRangeOverlap}?name={Uri.EscapeDataString(other.Name)}");
            }
        }

        return Result.Success();
    }
}

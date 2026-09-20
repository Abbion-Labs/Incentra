using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Domain.Entities.Lookup;
using VariableCompensation.Testing.Common.Fixtures;

namespace VariableCompensation.Testing.Common.Fakes;

public sealed class FakeEvaluationLookupRepository : IEvaluationLookupRepository
{
    private readonly IReadOnlyList<RatingLevel> ratingLevels;
    private readonly IReadOnlyList<DescriptiveRating> descriptiveRatings;

    public FakeEvaluationLookupRepository(
        IReadOnlyList<RatingLevel>? ratingLevels = null,
        IReadOnlyList<DescriptiveRating>? descriptiveRatings = null)
    {
        this.ratingLevels = ratingLevels ?? RatingLevelsFixture.Create();
        this.descriptiveRatings = descriptiveRatings ?? DescriptiveRatingsFixture.Create();
    }

    public Task<IReadOnlyList<RatingLevel>> GetRatingLevelsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(this.ratingLevels);

    public Task<IReadOnlyList<DescriptiveRating>> GetDescriptiveRatingsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(this.descriptiveRatings);

    public Task<IReadOnlyList<MeasureType>> GetMeasureTypesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MeasureType>>([
            new MeasureType { Id = 1, Code = "INITIATIVE", Name = "Inicijativa", IsActive = true },
        ]);

    public Task<bool> RatingLevelExistsAsync(long id, CancellationToken cancellationToken) =>
        Task.FromResult(this.ratingLevels.Any(r => r.Id == id));

    public Task<bool> MeasureTypeExistsAsync(long id, CancellationToken cancellationToken) =>
        Task.FromResult(id == 1);
}

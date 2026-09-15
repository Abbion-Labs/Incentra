using EvaluationEntity = VariableCompensation.Domain.Entities.Evaluation.Evaluation;
using VariableCompensation.Domain.Entities.Lookup;

namespace VariableCompensation.Application.Evaluation.Services;

public static class RatingLevelRules
{
    public const int NotRatedValue = 0;

    public static bool IsNotRated(RatingLevel level) => level.Value == NotRatedValue;

    public static bool IsNotRated(int value) => value == NotRatedValue;

    public static HashSet<long> GetNotRatedIds(IReadOnlyList<RatingLevel> ratingLevels) =>
        ratingLevels.Where(IsNotRated).Select(r => r.Id).ToHashSet();

    public static bool HasIncompleteRatings(EvaluationEntity evaluation, IReadOnlyList<RatingLevel> ratingLevels)
    {
        if (!evaluation.ConditionsFulfilled)
        {
            return false;
        }

        var notRatedIds = GetNotRatedIds(ratingLevels);
        if (evaluation.Goals.Any(g => notRatedIds.Contains(g.RatingLevelId))
            || evaluation.Measures.Any(m => notRatedIds.Contains(m.RatingLevelId)))
        {
            return true;
        }

        // Dok merila nisu uneta, ocena nije kompletna — prosek ostaje „/“.
        return evaluation.Goals.Count > 0 && evaluation.Measures.Count == 0;
    }
}

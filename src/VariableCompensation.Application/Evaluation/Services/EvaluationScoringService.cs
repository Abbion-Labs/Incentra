using EvaluationEntity = VariableCompensation.Domain.Entities.Evaluation.Evaluation;

using VariableCompensation.Domain.Entities.Lookup;



namespace VariableCompensation.Application.Evaluation.Services;



public sealed class EvaluationScoringService

{

    public void Recalculate(EvaluationEntity evaluation, IReadOnlyList<RatingLevel> ratingLevels, IReadOnlyList<DescriptiveRating> descriptiveRatings)

    {

        if (!evaluation.ConditionsFulfilled)

        {

            evaluation.GoalsAverage = null;

            evaluation.MeasuresAverage = null;

            evaluation.OverallAverage = null;

            evaluation.DescriptiveRatingId = null;

            return;

        }

        var notRatedIds = RatingLevelRules.GetNotRatedIds(ratingLevels);

        var valueById = ratingLevels.ToDictionary(r => r.Id, r => (decimal)r.Value);



        if (RatingLevelRules.HasIncompleteRatings(evaluation, ratingLevels))

        {

            evaluation.GoalsAverage = null;

            evaluation.MeasuresAverage = null;

            evaluation.OverallAverage = null;

            evaluation.DescriptiveRatingId = null;

            return;

        }



        evaluation.GoalsAverage = CalculateAverage(

            evaluation.Goals.Select(g => (g.RatingLevelId, g.Weight)).ToList(),

            valueById,

            notRatedIds);



        evaluation.MeasuresAverage = CalculateAverage(

            evaluation.Measures.Select(m => (m.RatingLevelId, (decimal?)null)).ToList(),

            valueById,

            notRatedIds);



        var parts = new List<decimal>();

        if (evaluation.GoalsAverage.HasValue)

        {

            parts.Add(evaluation.GoalsAverage.Value);

        }



        if (evaluation.MeasuresAverage.HasValue)

        {

            parts.Add(evaluation.MeasuresAverage.Value);

        }



        evaluation.OverallAverage = parts.Count > 0 ? Math.Round(parts.Average(), 4) : null;



        if (evaluation.OverallAverage is null)

        {

            evaluation.DescriptiveRatingId = null;

            return;

        }



        var average = evaluation.OverallAverage.Value;

        evaluation.DescriptiveRatingId = DescriptiveRatingBands.Resolve(descriptiveRatings, average)?.Id;

    }



    private static decimal? CalculateAverage(

        IReadOnlyList<(long RatingLevelId, decimal? Weight)> items,

        IReadOnlyDictionary<long, decimal> valueById,

        HashSet<long> notRatedIds)

    {

        if (items.Count == 0)

        {

            return null;

        }



        if (items.Any(i => notRatedIds.Contains(i.RatingLevelId)))

        {

            return null;

        }



        var rated = items

            .Where(i => valueById.ContainsKey(i.RatingLevelId) && !notRatedIds.Contains(i.RatingLevelId))

            .Select(i => (Value: valueById[i.RatingLevelId], i.Weight))

            .ToList();



        if (rated.Count == 0)

        {

            return null;

        }



        var totalWeight = rated.Where(r => r.Weight is > 0).Sum(r => r.Weight!.Value);

        if (totalWeight > 0)

        {

            var weighted = rated.Where(r => r.Weight is > 0).Sum(r => r.Value * r.Weight!.Value);

            return Math.Round(weighted / totalWeight, 4);

        }



        return Math.Round(rated.Average(r => r.Value), 4);

    }

}


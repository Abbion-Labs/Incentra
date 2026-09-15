using System.Text.Json;
using FluentAssertions;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain.Entities.Evaluation;
using VariableCompensation.Testing.Common.Builders;
using VariableCompensation.Testing.Common.Fixtures;

namespace VariableCompensation.Application.Tests.Evaluation;

[Trait("Category", "Unit")]
public class EvaluationScoringServiceTests
{
    private readonly EvaluationScoringService service = new();

    [Fact]
    public void Recalculate_WeightedGoalsAndSimpleMeasures_ComputesExpectedAverages()
    {
        var evaluation = new EvaluationBuilder()
            .AddGoal(RatingLevelsFixture.RatedLevelId(4), weight: 2)
            .AddGoal(RatingLevelsFixture.RatedLevelId(5), weight: 1)
            .AddMeasure(RatingLevelsFixture.RatedLevelId(4))
            .AddMeasure(RatingLevelsFixture.RatedLevelId(4))
            .Build();

        this.service.Recalculate(evaluation, RatingLevelsFixture.Create(), DescriptiveRatingsFixture.Create());

        evaluation.GoalsAverage.Should().Be(4.3333m);
        evaluation.MeasuresAverage.Should().Be(4.0m);
        evaluation.OverallAverage.Should().Be(4.1666m);
        evaluation.DescriptiveRatingId.Should().Be(4);
    }

    [Fact]
    public void Recalculate_NotRatedGoal_ClearsAverages()
    {
        var evaluation = new EvaluationBuilder()
            .AddGoal(RatingLevelsFixture.NotRatedId)
            .AddMeasure(RatingLevelsFixture.RatedLevelId(4))
            .Build();

        this.service.Recalculate(evaluation, RatingLevelsFixture.Create(), DescriptiveRatingsFixture.Create());

        evaluation.GoalsAverage.Should().BeNull();
        evaluation.MeasuresAverage.Should().BeNull();
        evaluation.OverallAverage.Should().BeNull();
        evaluation.DescriptiveRatingId.Should().BeNull();
    }

    [Fact]
    public void Recalculate_GoalsWithoutMeasures_ClearsAverages()
    {
        var evaluation = new EvaluationBuilder()
            .AddGoal(RatingLevelsFixture.RatedLevelId(3))
            .Build();

        this.service.Recalculate(evaluation, RatingLevelsFixture.Create(), DescriptiveRatingsFixture.Create());

        evaluation.OverallAverage.Should().BeNull();
        evaluation.DescriptiveRatingId.Should().BeNull();
    }

    [Fact]
    public void GoldenScoringCases_MatchExpectedOutputs()
    {
        var json = File.ReadAllText(GetGoldenPath("scoring-cases.json"));
        using var document = JsonDocument.Parse(json);

        foreach (var testCase in document.RootElement.EnumerateArray())
        {
            var name = testCase.GetProperty("name").GetString();
            var goals = testCase.GetProperty("goals");
            var measures = testCase.GetProperty("measures");
            var expected = testCase.GetProperty("expected");

            if (expected.TryGetProperty("incomplete", out var incompleteFlag) && incompleteFlag.GetBoolean())
            {
                var incompleteEvaluation = new EvaluationBuilder().Build();
                foreach (var goal in goals.EnumerateArray())
                {
                    incompleteEvaluation.Goals.Add(new EvaluationGoal
                    {
                        RatingLevelId = goal.GetProperty("ratingLevelId").GetInt64(),
                        Weight = goal.TryGetProperty("weight", out var weight) ? weight.GetDecimal() : null,
                        Description = "goal",
                        SortOrder = incompleteEvaluation.Goals.Count + 1,
                    });
                }

                RatingLevelRules.HasIncompleteRatings(incompleteEvaluation, RatingLevelsFixture.Create()).Should().BeTrue($"case {name}");
                continue;
            }

            var builder = new EvaluationBuilder();
            foreach (var goal in goals.EnumerateArray())
            {
                builder.AddGoal(
                    goal.GetProperty("ratingLevelId").GetInt64(),
                    goal.TryGetProperty("weight", out var weight) ? weight.GetDecimal() : null);
            }

            foreach (var measure in measures.EnumerateArray())
            {
                builder.AddMeasure(measure.GetProperty("ratingLevelId").GetInt64());
            }

            var evaluation = builder.Build();
            this.service.Recalculate(evaluation, RatingLevelsFixture.Create(), DescriptiveRatingsFixture.Create());

            AssertNullableDecimal(expected, "goalsAverage", evaluation.GoalsAverage, name);
            AssertNullableDecimal(expected, "measuresAverage", evaluation.MeasuresAverage, name);
            AssertNullableDecimal(expected, "overallAverage", evaluation.OverallAverage, name);

            if (expected.TryGetProperty("descriptiveRatingCode", out var codeElement) &&
                codeElement.ValueKind != JsonValueKind.Null)
            {
                var code = codeElement.GetString();
                var descriptive = DescriptiveRatingsFixture.Create().First(d => d.Code == code);
                evaluation.DescriptiveRatingId.Should().Be(descriptive.Id, $"case {name}");
            }
            else
            {
                evaluation.DescriptiveRatingId.Should().BeNull($"case {name}");
            }
        }
    }

    private static void AssertNullableDecimal(JsonElement expected, string property, decimal? actual, string? caseName)
    {
        if (!expected.TryGetProperty(property, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            actual.Should().BeNull($"case {caseName}");
            return;
        }

        actual.Should().Be(element.GetDecimal(), $"case {caseName}");
    }

    private static string GetGoldenPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "golden", fileName);
}

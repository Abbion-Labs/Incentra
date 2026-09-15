using FluentAssertions;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Testing.Common.Builders;
using VariableCompensation.Testing.Common.Fixtures;

namespace VariableCompensation.Application.Tests.Evaluation;

[Trait("Category", "Unit")]
public class RatingLevelRulesTests
{
  [Fact]
  public void HasIncompleteRatings_WhenGoalsExistWithoutMeasures_ReturnsTrue()
  {
    var evaluation = new EvaluationBuilder()
      .AddGoal(RatingLevelsFixture.RatedLevelId(3))
      .Build();
    var levels = RatingLevelsFixture.Create();

    RatingLevelRules.HasIncompleteRatings(evaluation, levels).Should().BeTrue();
  }

  [Fact]
  public void HasIncompleteRatings_WhenNotRatedPresent_ReturnsTrue()
  {
    var evaluation = new EvaluationBuilder()
      .AddGoal(RatingLevelsFixture.NotRatedId)
      .AddMeasure(RatingLevelsFixture.RatedLevelId(3))
      .Build();

    RatingLevelRules.HasIncompleteRatings(evaluation, RatingLevelsFixture.Create()).Should().BeTrue();
  }

  [Fact]
  public void HasIncompleteRatings_WhenFullyRated_ReturnsFalse()
  {
    var evaluation = new EvaluationBuilder()
      .AddGoal(RatingLevelsFixture.RatedLevelId(3))
      .AddMeasure(RatingLevelsFixture.RatedLevelId(4))
      .Build();

    RatingLevelRules.HasIncompleteRatings(evaluation, RatingLevelsFixture.Create()).Should().BeFalse();
  }
}

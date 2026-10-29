using FluentAssertions;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Testing.Common.Fixtures;

namespace VariableCompensation.Application.Tests.Evaluation;

[Trait("Category", "Unit")]
public class DescriptiveRatingBandsTests
{
    [Theory]
    [InlineData(0.0, "DOES_NOT_MEET")]
    [InlineData(1.995, "DOES_NOT_MEET")]
    [InlineData(2.0, "MEETS")]
    [InlineData(3.4983, "GOOD")]
    [InlineData(3.4999, "GOOD")]
    [InlineData(3.5, "EXCEEDS")]
    [InlineData(4.4999, "EXCEEDS")]
    [InlineData(4.5, "OUTSTANDING")]
    [InlineData(5.0, "OUTSTANDING")]
    public void Resolve_AverageBetweenBands_FallsIntoTheLowerOne(double average, string expectedCode)
    {
        var band = DescriptiveRatingBands.Resolve(DescriptiveRatingsFixture.Create(), (decimal)average);

        band!.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void Resolve_AboveTopBand_ReturnsNull()
    {
        DescriptiveRatingBands.Resolve(DescriptiveRatingsFixture.Create(), 5.01m).Should().BeNull();
    }

    [Fact]
    public void Resolve_BelowLowestBand_ReturnsNull()
    {
        var ratings = DescriptiveRatingsFixture.Create().Where(r => r.Code != "DOES_NOT_MEET").ToList();

        DescriptiveRatingBands.Resolve(ratings, 1.5m).Should().BeNull();
    }

    [Fact]
    public void Resolve_IgnoresInactiveBands()
    {
        var ratings = DescriptiveRatingsFixture.Create();
        ratings.Single(r => r.Code == "EXCEEDS").IsActive = false;

        DescriptiveRatingBands.Resolve(ratings, 4.0m)!.Code.Should().Be("GOOD");
    }
}

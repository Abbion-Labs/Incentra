using FluentAssertions;
using VariableCompensation.Application.Common;
using VariableCompensation.Domain.Entities.Hr;

namespace VariableCompensation.Application.Tests.Common;

[Trait("Category", "Unit")]
public class EmployeeNameSearchTests
{
    private static readonly Employee[] People =
    [
        new() { Id = 1, FirstName = "Luka", LastName = "Petrović" },
        new() { Id = 2, FirstName = "Ana", LastName = "Jovanović" },
        new() { Id = 3, FirstName = "Luka", LastName = "Jovanović" },
    ];

    [Theory]
    [InlineData("Luka", new long[] { 1, 3 })]
    [InlineData("Petrović", new long[] { 1 })]
    [InlineData("Luka P", new long[] { 1 })]
    [InlineData("Luka Petrović", new long[] { 1 })]
    [InlineData("Petrović Luka", new long[] { 1 })]
    [InlineData("  luka   petrović ", new long[] { 1 })]
    [InlineData("Jovanović", new long[] { 2, 3 })]
    [InlineData("Milan", new long[] { })]
    public void Apply_MatchesTheFullNameInBothOrders(string search, long[] expectedIds)
    {
        var result = EmployeeNameSearch.Apply(People.AsQueryable(), search);

        result.Select(e => e.Id).Should().BeEquivalentTo(expectedIds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Apply_WithoutATerm_LeavesTheQueryAlone(string? search)
    {
        var result = EmployeeNameSearch.Apply(People.AsQueryable(), search);

        result.Should().HaveCount(People.Length);
    }

    [Fact]
    public void Normalize_CollapsesWhitespaceAndLowerCases()
    {
        EmployeeNameSearch.Normalize("  Luka   PETROVIĆ ").Should().Be("luka petrović");
        EmployeeNameSearch.Normalize("   ").Should().BeNull();
        EmployeeNameSearch.Normalize(null).Should().BeNull();
    }
}

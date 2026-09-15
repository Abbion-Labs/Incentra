using VariableCompensation.Domain.Entities.Lookup;

namespace VariableCompensation.Testing.Common.Fixtures;

public static class DescriptiveRatingsFixture
{
    public static IReadOnlyList<DescriptiveRating> Create() =>
    [
        new() { Id = 1, Code = "DOES_NOT_MEET", Name = "Ne zadovoljava", MinAverage = 0.00m, MaxAverage = 1.99m, SortOrder = 1 },
        new() { Id = 2, Code = "MEETS", Name = "Zadovoljava", MinAverage = 2.00m, MaxAverage = 2.99m, SortOrder = 2 },
        new() { Id = 3, Code = "GOOD", Name = "Dobar", MinAverage = 3.00m, MaxAverage = 3.49m, SortOrder = 3 },
        new() { Id = 4, Code = "EXCEEDS", Name = "Ističe se", MinAverage = 3.50m, MaxAverage = 4.49m, SortOrder = 4 },
        new() { Id = 5, Code = "OUTSTANDING", Name = "Naročito se ističe", MinAverage = 4.50m, MaxAverage = 5.00m, SortOrder = 5 },
    ];
}

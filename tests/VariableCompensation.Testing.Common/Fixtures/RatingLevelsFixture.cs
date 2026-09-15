using VariableCompensation.Domain.Entities.Lookup;

namespace VariableCompensation.Testing.Common.Fixtures;

public static class RatingLevelsFixture
{
    public static IReadOnlyList<RatingLevel> Create() =>
    [
        new() { Id = 1, Value = 0, Label = "/", Description = "Nije ocenjeno." },
        new() { Id = 2, Value = 1, Label = "Ne zadovoljava" },
        new() { Id = 3, Value = 2, Label = "Minimalno zadovoljava" },
        new() { Id = 4, Value = 3, Label = "Zadovoljava" },
        new() { Id = 5, Value = 4, Label = "Ističe se" },
        new() { Id = 6, Value = 5, Label = "Naročito se ističe" },
    ];

    public static long NotRatedId => 1;

    public static long RatedLevelId(int value) => value switch
    {
        1 => 2,
        2 => 3,
        3 => 4,
        4 => 5,
        5 => 6,
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };
}

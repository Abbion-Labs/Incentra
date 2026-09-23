using FluentAssertions;
using VariableCompensation.Application.Common;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Lookup;

namespace VariableCompensation.Application.Tests.Common;

[Trait("Category", "Unit")]
public class EditVersionTests
{
    [Fact]
    public void Claim_TheCurrentVersion_MovesItOn()
    {
        var record = new JobPosition { Version = 3 };

        var result = EditVersion.Claim(record, 3);

        result.IsSuccess.Should().BeTrue();
        record.Version.Should().Be(4);
    }

    [Fact]
    public void Claim_AnOutdatedVersion_IsAConflict()
    {
        var record = new JobPosition { Version = 3 };

        var result = EditVersion.Claim(record, 2);

        result.Error.Should().Be(ErrorCodes.ConcurrencyConflict);
        record.Version.Should().Be(3);
    }

    [Fact]
    public void Claim_WithoutAVersion_IsRejected()
    {
        var record = new JobPosition { Version = 0 };

        var result = EditVersion.Claim(record, null);

        result.Error.Should().Be(ErrorCodes.VersionRequired);
        record.Version.Should().Be(0);
    }
}

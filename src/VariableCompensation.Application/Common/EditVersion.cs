using CSharpFunctionalExtensions;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Common;

namespace VariableCompensation.Application.Common;

/// <summary>
/// The check every edit of a versioned record goes through. The edit states the version it was made from; when the
/// record has moved on since, the edit is refused rather than undoing the newer change. On success the version
/// moves on too, and the database refuses the save if another edit got in between.
/// </summary>
public static class EditVersion
{
    public static Result Claim(IVersioned record, int? expectedVersion)
    {
        if (expectedVersion is null)
        {
            return Result.Failure(ErrorCodes.VersionRequired);
        }

        if (record.Version != expectedVersion)
        {
            return Result.Failure(ErrorCodes.ConcurrencyConflict);
        }

        record.Version++;
        return Result.Success();
    }
}

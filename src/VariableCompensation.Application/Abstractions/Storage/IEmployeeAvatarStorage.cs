namespace VariableCompensation.Application.Abstractions.Storage;

public interface IEmployeeAvatarStorage
{
    /// <summary>
    /// Stores a new picture under a location of its own and returns its URL. The current picture is left alone, so
    /// it survives a failed upload; the caller removes it once the new one is in place.
    /// </summary>
    Task<string> SaveAsync(long employeeId, Stream content, string contentType, CancellationToken cancellationToken);

    Task DeleteIfExistsAsync(string? avatarUrl, CancellationToken cancellationToken);
}

namespace VariableCompensation.Application.Abstractions.Storage;

public interface IEmployeeAvatarStorage
{
    Task<string> SaveAsync(long employeeId, Stream content, string contentType, CancellationToken cancellationToken);

    Task DeleteIfExistsAsync(string? avatarUrl, CancellationToken cancellationToken);
}

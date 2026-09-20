using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Domain.Entities.Lookup;

namespace VariableCompensation.Application.Abstractions.Persistence;

public interface IOrganizationUnitRepository
{
    Task<OrganizationUnit?> FindByIdAsync(long id, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, long? excludeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OrganizationUnit>> GetAllAsync(bool? isActive, CancellationToken cancellationToken);

    Task AddAsync(OrganizationUnit entity, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IJobPositionRepository
{
    Task<JobPosition?> FindByIdAsync(long id, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, long? excludeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<JobPosition>> GetAllAsync(bool? isActive, CancellationToken cancellationToken);

    Task AddAsync(JobPosition entity, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IEducationLevelRepository
{
    Task<EducationLevel?> FindByIdAsync(long id, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, long? excludeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<EducationLevel>> GetAllAsync(bool? isActive, CancellationToken cancellationToken);

    Task AddAsync(EducationLevel entity, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IEmployeeRepository
{
    Task<Employee?> FindByIdAsync(long id, CancellationToken cancellationToken);

    Task<Employee?> FindByIdForUpdateAsync(long id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        long? organizationUnitId,
        long? evaluatorEmployeeId,
        string? search,
        bool? isActive,
        long? controllerEmployeeId,
        short? goalsYear,
        byte? goalsQuarter,
        string? goalsBucket,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetWithoutCurrentSalaryPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken);

    Task<Employee?> FindByUserIdAsync(long userId, CancellationToken cancellationToken);

    Task<bool> IsUserLinkedToAnotherEmployeeAsync(long userId, long excludeEmployeeId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(long id, CancellationToken cancellationToken);

    Task<Employee?> FindByIdWithEvaluatorAsync(long id, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<long, Employee>> GetEmployeesByUserIdsAsync(CancellationToken cancellationToken);

    Task<bool> HasSubordinatesAsync(long evaluatorEmployeeId, CancellationToken cancellationToken);

    Task AddAsync(Employee entity, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

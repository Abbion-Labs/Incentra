using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Abstractions.Security;
using VariableCompensation.Application.Common.Models;
using VariableCompensation.Application.Hr.Models;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Hr.EmployeeSalaries.Queries;

public sealed record GetEmployeeSalariesQuery : IRequest<Result<IReadOnlyList<EmployeeSalaryResponse>>>;

public sealed record GetEmployeeSalariesPagedQuery(int Page, int PageSize, string? Search)
    : IRequest<Result<PagedResult<EmployeeSalaryResponse>>>;

public sealed class GetEmployeeSalariesPagedQueryHandler
    : IRequestHandler<GetEmployeeSalariesPagedQuery, Result<PagedResult<EmployeeSalaryResponse>>>
{
    private readonly IEmployeeSalaryRepository salaryRepository;
    private readonly ISensitiveDataEncryptionService encryptionService;
    private readonly IAuditLogWriter auditLogWriter;
    private readonly ICurrentUserService currentUserService;

    public GetEmployeeSalariesPagedQueryHandler(
        IEmployeeSalaryRepository salaryRepository,
        ISensitiveDataEncryptionService encryptionService,
        IAuditLogWriter auditLogWriter,
        ICurrentUserService currentUserService)
    {
        this.salaryRepository = salaryRepository;
        this.encryptionService = encryptionService;
        this.auditLogWriter = auditLogWriter;
        this.currentUserService = currentUserService;
    }

    public async Task<Result<PagedResult<EmployeeSalaryResponse>>> Handle(
        GetEmployeeSalariesPagedQuery request,
        CancellationToken cancellationToken)
    {
        if (!this.currentUserService.IsInRole(RoleCodes.Payroll))
        {
            return Result.Failure<PagedResult<EmployeeSalaryResponse>>("Forbidden.");
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 30 : request.PageSize;

        var (rows, totalCount) = await this.salaryRepository.GetCurrentPagedAsync(
            page,
            pageSize,
            request.Search,
            cancellationToken);

        if (page == 1)
        {
            await this.auditLogWriter.WriteAsync(
                "EmployeeSalary",
                0,
                "ListRead",
                null,
                $"{{\"count\":{totalCount}}}",
                cancellationToken);
        }

        return Result.Success(new PagedResult<EmployeeSalaryResponse>
        {
            Items = GetEmployeeSalariesQueryHandler.MapRows(rows, this.encryptionService),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        });
    }
}

public sealed class GetEmployeeSalariesQueryHandler : IRequestHandler<GetEmployeeSalariesQuery, Result<IReadOnlyList<EmployeeSalaryResponse>>>
{
    private readonly IEmployeeSalaryRepository salaryRepository;
    private readonly ISensitiveDataEncryptionService encryptionService;
    private readonly IAuditLogWriter auditLogWriter;
    private readonly ICurrentUserService currentUserService;

    public GetEmployeeSalariesQueryHandler(
        IEmployeeSalaryRepository salaryRepository,
        ISensitiveDataEncryptionService encryptionService,
        IAuditLogWriter auditLogWriter,
        ICurrentUserService currentUserService)
    {
        this.salaryRepository = salaryRepository;
        this.encryptionService = encryptionService;
        this.auditLogWriter = auditLogWriter;
        this.currentUserService = currentUserService;
    }

    public async Task<Result<IReadOnlyList<EmployeeSalaryResponse>>> Handle(
        GetEmployeeSalariesQuery request,
        CancellationToken cancellationToken)
    {
        if (!this.currentUserService.IsInRole(RoleCodes.Payroll))
        {
            return Result.Failure<IReadOnlyList<EmployeeSalaryResponse>>("Forbidden.");
        }

        var rows = await this.salaryRepository.GetAllCurrentWithEmployeesAsync(cancellationToken);
        await this.auditLogWriter.WriteAsync(
            "EmployeeSalary",
            0,
            "ListRead",
            null,
            $"{{\"count\":{rows.Count}}}",
            cancellationToken);

        return Result.Success(MapRows(rows, this.encryptionService));
    }

    internal static IReadOnlyList<EmployeeSalaryResponse> MapRows(
        IReadOnlyList<EmployeeSalary> rows,
        ISensitiveDataEncryptionService encryptionService) =>
        rows.Select(row => MapRow(row, encryptionService)).ToList();

    internal static EmployeeSalaryResponse MapRow(EmployeeSalary row, ISensitiveDataEncryptionService encryptionService) =>
        new()
        {
            Id = row.Id,
            EmployeeId = row.EmployeeId,
            EmployeeFullName = row.Employee.FullName,
            OrganizationUnitName = row.Employee.OrganizationUnit?.Name ?? string.Empty,
            Points = row.Points,
            SalaryPerPoint = encryptionService.DecryptDecimal(row.EncryptedSalaryPerPoint),
            Currency = row.Currency,
            EffectiveFrom = row.EffectiveFrom,
            EffectiveTo = row.EffectiveTo,
            IsCurrent = row.EffectiveTo is null,
            UpdatedAt = row.UpdatedAt,
        };
}

public sealed record GetEmployeeSalaryHistoryQuery(long EmployeeId) : IRequest<Result<IReadOnlyList<EmployeeSalaryResponse>>>;

public sealed class GetEmployeeSalaryHistoryQueryHandler
    : IRequestHandler<GetEmployeeSalaryHistoryQuery, Result<IReadOnlyList<EmployeeSalaryResponse>>>
{
    private readonly IEmployeeSalaryRepository salaryRepository;
    private readonly ISensitiveDataEncryptionService encryptionService;
    private readonly ICurrentUserService currentUserService;

    public GetEmployeeSalaryHistoryQueryHandler(
        IEmployeeSalaryRepository salaryRepository,
        ISensitiveDataEncryptionService encryptionService,
        ICurrentUserService currentUserService)
    {
        this.salaryRepository = salaryRepository;
        this.encryptionService = encryptionService;
        this.currentUserService = currentUserService;
    }

    public async Task<Result<IReadOnlyList<EmployeeSalaryResponse>>> Handle(
        GetEmployeeSalaryHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (!this.currentUserService.IsInRole(RoleCodes.Payroll))
        {
            return Result.Failure<IReadOnlyList<EmployeeSalaryResponse>>("Forbidden.");
        }

        var rows = await this.salaryRepository.GetHistoryByEmployeeIdAsync(request.EmployeeId, cancellationToken);
        if (rows.Count == 0)
        {
            return Result.Failure<IReadOnlyList<EmployeeSalaryResponse>>("Employee not found or has no salary history.");
        }

        return Result.Success(GetEmployeeSalariesQueryHandler.MapRows(rows, this.encryptionService));
    }
}

public sealed record GetEmployeesWithoutSalaryQuery : IRequest<Result<IReadOnlyList<EmployeeSalaryEmployeeOption>>>;

public sealed record GetEmployeesWithoutSalaryPagedQuery(int Page, int PageSize, string? Search)
    : IRequest<Result<PagedResult<EmployeeSalaryEmployeeOption>>>;

public sealed class EmployeeSalaryEmployeeOption
{
    public long EmployeeId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string OrganizationUnitName { get; init; } = string.Empty;
}

public sealed class GetEmployeesWithoutSalaryQueryHandler
    : IRequestHandler<GetEmployeesWithoutSalaryQuery, Result<IReadOnlyList<EmployeeSalaryEmployeeOption>>>
{
    private readonly IEmployeeRepository employeeRepository;
    private readonly ICurrentUserService currentUserService;

    public GetEmployeesWithoutSalaryQueryHandler(
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService)
    {
        this.employeeRepository = employeeRepository;
        this.currentUserService = currentUserService;
    }

    public async Task<Result<IReadOnlyList<EmployeeSalaryEmployeeOption>>> Handle(
        GetEmployeesWithoutSalaryQuery request,
        CancellationToken cancellationToken)
    {
        if (!this.currentUserService.IsInRole(RoleCodes.Payroll))
        {
            return Result.Failure<IReadOnlyList<EmployeeSalaryEmployeeOption>>("Forbidden.");
        }

        var (employees, _) = await this.employeeRepository.GetWithoutCurrentSalaryPagedAsync(
            1, 1000, null, cancellationToken);

        var options = employees
            .Select(e => new EmployeeSalaryEmployeeOption
            {
                EmployeeId = e.Id,
                FullName = e.FullName,
                OrganizationUnitName = e.OrganizationUnit?.Name ?? string.Empty,
            })
            .ToList();

        return Result.Success<IReadOnlyList<EmployeeSalaryEmployeeOption>>(options);
    }
}

public sealed class GetEmployeesWithoutSalaryPagedQueryHandler
    : IRequestHandler<GetEmployeesWithoutSalaryPagedQuery, Result<PagedResult<EmployeeSalaryEmployeeOption>>>
{
    private readonly IEmployeeRepository employeeRepository;
    private readonly ICurrentUserService currentUserService;

    public GetEmployeesWithoutSalaryPagedQueryHandler(
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService)
    {
        this.employeeRepository = employeeRepository;
        this.currentUserService = currentUserService;
    }

    public async Task<Result<PagedResult<EmployeeSalaryEmployeeOption>>> Handle(
        GetEmployeesWithoutSalaryPagedQuery request,
        CancellationToken cancellationToken)
    {
        if (!this.currentUserService.IsInRole(RoleCodes.Payroll))
        {
            return Result.Failure<PagedResult<EmployeeSalaryEmployeeOption>>("Forbidden.");
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 100 : request.PageSize;

        var (employees, totalCount) = await this.employeeRepository.GetWithoutCurrentSalaryPagedAsync(
            page,
            pageSize,
            request.Search,
            cancellationToken);

        return Result.Success(new PagedResult<EmployeeSalaryEmployeeOption>
        {
            Items = employees
                .Select(e => new EmployeeSalaryEmployeeOption
                {
                    EmployeeId = e.Id,
                    FullName = e.FullName,
                    OrganizationUnitName = e.OrganizationUnit?.Name ?? string.Empty,
                })
                .ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        });
    }
}

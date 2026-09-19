using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Common.Models;
using VariableCompensation.Application.Hr.Models;
using VariableCompensation.Application.Hr.Services;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Hr.Employees.Queries;

public sealed record GetEmployeesQuery(
    int Page,
    int PageSize,
    long? OrganizationUnitId,
    long? EvaluatorEmployeeId,
    string? Search,
    bool? IsActive,
    short? GoalsYear,
    byte? GoalsQuarter,
    string? GoalsBucket) : IRequest<PagedResult<EmployeeResponse>>;

public sealed class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, PagedResult<EmployeeResponse>>
{
    private readonly IEmployeeRepository repository;
    private readonly ICurrentUserService currentUserService;
    private readonly ICurrentEmployeeContext currentEmployeeContext;

    public GetEmployeesQueryHandler(
        IEmployeeRepository repository,
        ICurrentUserService currentUserService,
        ICurrentEmployeeContext currentEmployeeContext)
    {
        this.repository = repository;
        this.currentUserService = currentUserService;
        this.currentEmployeeContext = currentEmployeeContext;
    }

    public async Task<PagedResult<EmployeeResponse>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        if (!this.currentUserService.IsAdmin &&
            !this.currentUserService.IsInRole(RoleCodes.Evaluator) &&
            !this.currentUserService.IsInRole(RoleCodes.Controller))
        {
            var ownEmployeeId = await this.currentEmployeeContext.GetEmployeeIdAsync(cancellationToken);
            var own = ownEmployeeId is null
                ? null
                : await this.repository.FindByIdAsync(ownEmployeeId.Value, cancellationToken);

            return new PagedResult<EmployeeResponse>
            {
                Items = own is null ? [] : [HrMappings.ToResponse(own)],
                Page = page,
                PageSize = pageSize,
                TotalCount = own is null ? 0 : 1,
            };
        }

        var evaluatorEmployeeId = request.EvaluatorEmployeeId;
        long? controllerEmployeeId = null;
        if (!this.currentUserService.IsAdmin &&
            this.currentUserService.IsInRole(RoleCodes.Evaluator))
        {
            var currentId = await this.currentEmployeeContext.GetEmployeeIdAsync(cancellationToken);
            evaluatorEmployeeId = currentId ?? -1;
        }
        else if (!this.currentUserService.IsAdmin &&
                 this.currentUserService.IsInRole(RoleCodes.Controller))
        {
            var currentId = await this.currentEmployeeContext.GetEmployeeIdAsync(cancellationToken);
            controllerEmployeeId = currentId ?? -1;
        }

        var (items, totalCount) = await this.repository.GetPagedAsync(
            page,
            pageSize,
            request.OrganizationUnitId,
            evaluatorEmployeeId == -1 ? -1 : evaluatorEmployeeId,
            request.Search,
            request.IsActive,
            controllerEmployeeId == -1 ? -1 : controllerEmployeeId,
            request.GoalsYear,
            request.GoalsQuarter,
            request.GoalsBucket,
            cancellationToken);

        if (evaluatorEmployeeId == -1 || controllerEmployeeId == -1)
        {
            return new PagedResult<EmployeeResponse> { Page = page, PageSize = pageSize };
        }

        return new PagedResult<EmployeeResponse>
        {
            Items = items.Select(HrMappings.ToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}

public sealed record GetEmployeeByIdQuery(long Id) : IRequest<Result<EmployeeResponse>>;

public sealed class GetEmployeeByIdQueryHandler : IRequestHandler<GetEmployeeByIdQuery, Result<EmployeeResponse>>
{
    private readonly IEmployeeRepository repository;
    private readonly EmployeeAccessService employeeAccessService;

    public GetEmployeeByIdQueryHandler(IEmployeeRepository repository, EmployeeAccessService employeeAccessService)
    {
        this.repository = repository;
        this.employeeAccessService = employeeAccessService;
    }

    public async Task<Result<EmployeeResponse>> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await this.repository.FindByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<EmployeeResponse>(ErrorCodes.EmployeeNotFound);
        }

        var access = await this.employeeAccessService.EnsureCanViewAsync(entity, cancellationToken);
        if (access.IsFailure)
        {
            return Result.Failure<EmployeeResponse>(access.Error);
        }

        return HrMappings.ToResponse(entity);
    }
}

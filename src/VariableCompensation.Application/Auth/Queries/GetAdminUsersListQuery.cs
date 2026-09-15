using MediatR;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Auth.Models;

namespace VariableCompensation.Application.Auth.Queries;

public sealed record GetAdminUsersListQuery : IRequest<IReadOnlyList<AdminUserListItemResponse>>;

public sealed class GetAdminUsersListQueryHandler : IRequestHandler<GetAdminUsersListQuery, IReadOnlyList<AdminUserListItemResponse>>
{
    private readonly IUserRepository userRepository;
    private readonly IEmployeeRepository employeeRepository;

    public GetAdminUsersListQueryHandler(IUserRepository userRepository, IEmployeeRepository employeeRepository)
    {
        this.userRepository = userRepository;
        this.employeeRepository = employeeRepository;
    }

    public async Task<IReadOnlyList<AdminUserListItemResponse>> Handle(
        GetAdminUsersListQuery request,
        CancellationToken cancellationToken)
    {
        var users = await this.userRepository.GetAllWithRolesAsync(cancellationToken);
        var employeesByUserId = await this.employeeRepository.GetEmployeesByUserIdsAsync(cancellationToken);

        return users
            .Select(user =>
            {
                employeesByUserId.TryGetValue(user.Id, out var employee);
                return new AdminUserListItemResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    Roles = user.UserRoles.Select(ur => ur.Role.Code).OrderBy(code => code).ToList(),
                    IsActive = user.IsActive,
                    EmployeeId = employee?.Id,
                    EmployeeFullName = employee?.FullName,
                };
            })
            .OrderBy(item => item.Email)
            .ToList();
    }
}

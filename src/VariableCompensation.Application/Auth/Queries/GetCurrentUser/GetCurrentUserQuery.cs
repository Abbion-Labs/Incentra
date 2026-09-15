using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Auth;
using VariableCompensation.Application.Auth.Commands.Login;
using VariableCompensation.Application.Auth.Models;
using VariableCompensation.Domain;

namespace VariableCompensation.Application.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<Result<UserProfileResponse>>;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserProfileResponse>>
{
    private readonly ICurrentUserService currentUserService;
    private readonly IUserRepository userRepository;
    private readonly IEmployeeRepository employeeRepository;

    public GetCurrentUserQueryHandler(
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository)
    {
        this.currentUserService = currentUserService;
        this.userRepository = userRepository;
        this.employeeRepository = employeeRepository;
    }

    public async Task<Result<UserProfileResponse>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!this.currentUserService.IsAuthenticated || this.currentUserService.UserId is null)
        {
            return Result.Failure<UserProfileResponse>(ErrorCodes.UserNotAuthenticated);
        }

        var user = await this.userRepository.FindByIdWithRolesAsync(this.currentUserService.UserId.Value, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Failure<UserProfileResponse>(ErrorCodes.UserNotFound);
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Code).ToList();
        var employee = await this.employeeRepository.FindByUserIdAsync(user.Id, cancellationToken);
        return Result.Success(UserProfileMapper.Map(user, roles, employee));
    }
}

using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Auth.Models;
using VariableCompensation.Domain;

namespace VariableCompensation.Application.Auth.Commands.SelectRole;

/// <summary>
/// Moves the signed-in user into another of their roles: ends the session they are in, starts one that carries
/// only the chosen role, and remembers the choice for the next sign-in. Ending the old session also puts its
/// access token out of use at once, so nothing is left running in the previous role.
/// </summary>
public sealed record SelectRoleCommand(string RoleCode) : IRequest<Result<AuthResponse>>;

public sealed class SelectRoleCommandHandler : IRequestHandler<SelectRoleCommand, Result<AuthResponse>>
{
    private readonly ICurrentUserService currentUserService;
    private readonly IUserRepository userRepository;
    private readonly AuthSessionIssuer sessionIssuer;

    public SelectRoleCommandHandler(
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        AuthSessionIssuer sessionIssuer)
    {
        this.currentUserService = currentUserService;
        this.userRepository = userRepository;
        this.sessionIssuer = sessionIssuer;
    }

    public async Task<Result<AuthResponse>> Handle(SelectRoleCommand request, CancellationToken cancellationToken)
    {
        if (this.currentUserService.UserId is null)
        {
            return Result.Failure<AuthResponse>(ErrorCodes.UserNotAuthenticated);
        }

        var user = await this.userRepository.FindByIdWithRolesAsync(this.currentUserService.UserId.Value, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthResponse>(ErrorCodes.UserInactive);
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Code).ToList();
        var role = ActiveRoles.Find(roles, request.RoleCode);
        if (role is null)
        {
            return Result.Failure<AuthResponse>(ErrorCodes.RoleNotAssigned);
        }

        // The session that asked is the one being replaced; the access token says which.
        if (this.currentUserService.SessionId is Guid sessionId)
        {
            await this.userRepository.RevokeSessionAsync(sessionId, cancellationToken);
        }

        // Only a deliberate choice is worth remembering for the next sign-in.
        user.LastActiveRoleCode = role;
        var authResponse = await this.sessionIssuer.StartSessionAsync(user, role, cancellationToken);
        await this.userRepository.SaveChangesAsync(cancellationToken);
        return Result.Success(authResponse);
    }
}

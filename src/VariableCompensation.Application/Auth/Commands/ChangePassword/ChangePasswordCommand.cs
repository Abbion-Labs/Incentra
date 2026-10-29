using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Domain;

namespace VariableCompensation.Application.Auth.Commands.ChangePassword;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result>;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly ICurrentUserService currentUserService;
    private readonly IUserRepository userRepository;
    private readonly IPasswordHasher passwordHasher;

    public ChangePasswordCommandHandler(
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        this.currentUserService = currentUserService;
        this.userRepository = userRepository;
        this.passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (this.currentUserService.UserId is null)
        {
            return Result.Failure(ErrorCodes.UserNotAuthenticated);
        }

        if (request.NewPassword.Length < 8)
        {
            return Result.Failure(ErrorCodes.PasswordTooShort);
        }

        var user = await this.userRepository.FindByIdForUpdateAsync(this.currentUserService.UserId.Value, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Failure(ErrorCodes.UserNotFound);
        }

        if (!this.passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure(ErrorCodes.CurrentPasswordIncorrect);
        }

        user.PasswordHash = this.passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // Every other device is signed out. This one stays signed in: it has just proven the current password,
        // and ending its session would log the user out shortly after a successful change.
        if (this.currentUserService.SessionId is { } sessionId)
        {
            await this.userRepository.RevokeOtherSessionsAsync(user.Id, sessionId, cancellationToken);
        }
        else
        {
            await this.userRepository.RevokeAllRefreshTokensAsync(user.Id, cancellationToken);
        }

        await this.userRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

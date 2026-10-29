using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Auth.Commands.Login;

namespace VariableCompensation.Application.Auth.Commands.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest<Result>;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IUserRepository userRepository;

    public LogoutCommandHandler(IUserRepository userRepository)
    {
        this.userRepository = userRepository;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result.Success();
        }

        var storedToken = await this.userRepository.FindRefreshTokenByHashAsync(
            LoginCommandHandler.HashToken(request.RefreshToken),
            cancellationToken);

        if (storedToken is not null)
        {
            // Ends the whole sign-in, not only the presented token: every tab of the browser shares the session.
            await this.userRepository.RevokeSessionAsync(storedToken.SessionId, cancellationToken);
            await this.userRepository.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}

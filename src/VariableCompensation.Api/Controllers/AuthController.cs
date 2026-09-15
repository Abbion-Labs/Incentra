using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VariableCompensation.Api.Contracts.Auth;
using VariableCompensation.Application.Auth.Commands.ChangePassword;
using VariableCompensation.Application.Auth.Commands.Login;
using VariableCompensation.Application.Auth.Commands.RefreshToken;
using VariableCompensation.Application.Auth.Commands.RegisterUser;
using VariableCompensation.Application.Auth.Commands.UpdateNotificationPreferences;
using VariableCompensation.Application.Auth.Queries.GetCurrentUser;

namespace VariableCompensation.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator mediator;

    public AuthController(IMediator mediator)
    {
        this.mediator = mediator;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        return result.IsSuccess ? this.Ok(result.Value) : this.Unauthorized(new { error = result.Error });
    }

    [HttpPost("register")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var roleCodes = request.RoleCodes.Count > 0
            ? request.RoleCodes
            : string.IsNullOrWhiteSpace(request.RoleCode)
                ? Array.Empty<string>()
                : [request.RoleCode];

        var result = await this.mediator.Send(
            new RegisterUserCommand(request.Email, request.Password, roleCodes),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new RefreshTokenCommand(request.RefreshToken), cancellationToken);
        return result.IsSuccess ? this.Ok(result.Value) : this.Unauthorized(new { error = result.Error });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetCurrentUserQuery(), cancellationToken);
        return result.IsSuccess ? this.Ok(result.Value) : this.NotFound(new { error = result.Error });
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new ChangePasswordCommand(request.CurrentPassword, request.NewPassword),
            cancellationToken);

        return result.IsSuccess ? this.Ok() : this.BadRequest(new { error = result.Error });
    }

    [HttpPut("notification-preferences")]
    [Authorize]
    public async Task<IActionResult> UpdateNotificationPreferences(
        [FromBody] UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new UpdateNotificationPreferencesCommand(request.EmailNotificationsEnabled),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }
}

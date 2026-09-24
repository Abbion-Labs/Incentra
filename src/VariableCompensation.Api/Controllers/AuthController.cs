using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using VariableCompensation.Api.Contracts.Auth;
using VariableCompensation.Application.Auth.Commands.ChangePassword;
using VariableCompensation.Application.Auth.Commands.Login;
using VariableCompensation.Application.Auth.Commands.Logout;
using VariableCompensation.Application.Auth.Commands.RefreshToken;
using VariableCompensation.Application.Auth.Commands.RegisterUser;
using VariableCompensation.Application.Auth.Commands.SelectRole;
using VariableCompensation.Application.Auth.Commands.UpdateNotificationPreferences;
using VariableCompensation.Application.Auth.Models;
using VariableCompensation.Application.Auth.Queries.GetCurrentUser;
using VariableCompensation.Domain;

namespace VariableCompensation.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private const string RefreshCookieName = "vc_refresh";
    private const string RefreshCookiePath = "/api/auth";

    private readonly IMediator mediator;
    private readonly IConfiguration configuration;

    public AuthController(IMediator mediator, IConfiguration configuration)
    {
        this.mediator = mediator;
        this.configuration = configuration;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        if (result.IsSuccess)
        {
            this.SetRefreshCookie(result.Value);
            return this.Ok(result.Value);
        }

        return result.Error.StartsWith(ErrorCodes.TooManyLoginAttempts, StringComparison.Ordinal)
            ? this.StatusCode(StatusCodes.Status429TooManyRequests, new { error = result.Error })
            : this.Unauthorized(new { error = result.Error });
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
            new RegisterUserCommand(
                request.Email,
                request.Password,
                roleCodes,
                request.EmployeeId,
                request.ControllerEmployeeId),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshTokenRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new RefreshTokenCommand(this.ReadRefreshToken(request)), cancellationToken);
        if (result.IsFailure)
        {
            // The cookie is deliberately left alone. A failure here can simply mean another tab
            // rotated the token first, in which case the cookie already holds the valid one and
            // deleting it would sign every tab out.
            return this.Unauthorized(new { error = result.Error });
        }

        this.SetRefreshCookie(result.Value);
        return this.Ok(result.Value);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshTokenRequest? request,
        CancellationToken cancellationToken)
    {
        await this.mediator.Send(new LogoutCommand(this.ReadRefreshToken(request)), cancellationToken);
        this.Response.Cookies.Delete(RefreshCookieName, this.CreateRefreshCookieOptions());
        return this.NoContent();
    }

    [HttpPost("select-role")]
    [Authorize]
    public async Task<IActionResult> SelectRole([FromBody] SelectRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new SelectRoleCommand(request.RoleCode), cancellationToken);
        if (result.IsFailure)
        {
            return this.BadRequest(new { error = result.Error });
        }

        // The new session comes with its own refresh token, and the old one has just been revoked.
        this.SetRefreshCookie(result.Value);
        return this.Ok(result.Value);
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

    /// <summary>
    /// The refresh token normally travels in an HttpOnly cookie. The request body is kept as a fallback
    /// so the endpoint stays usable from Swagger and manual tooling.
    /// </summary>
    private string ReadRefreshToken(RefreshTokenRequest? body) =>
        this.Request.Cookies[RefreshCookieName] is { Length: > 0 } cookie ? cookie : body?.RefreshToken ?? string.Empty;

    private void SetRefreshCookie(AuthResponse auth)
    {
        var options = this.CreateRefreshCookieOptions();
        options.Expires = new DateTimeOffset(DateTime.SpecifyKind(auth.RefreshTokenExpiresAt, DateTimeKind.Utc));
        this.Response.Cookies.Append(RefreshCookieName, auth.RefreshToken, options);
    }

    private CookieOptions CreateRefreshCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = this.configuration.GetValue("RefreshCookie:Secure", true),
        SameSite = SameSiteMode.Strict,
        Path = RefreshCookiePath,
    };
}

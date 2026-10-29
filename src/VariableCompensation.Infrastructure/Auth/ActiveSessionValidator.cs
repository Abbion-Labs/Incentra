using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using VariableCompensation.Application.Abstractions.Persistence;

namespace VariableCompensation.Infrastructure.Auth;

/// <summary>
/// A valid signature and expiry only prove that an access token was issued, not that its session is still going.
/// Checked on every request, so signing out, a password change on another device, a role change or deactivating
/// the account end the access tokens at once instead of when they expire.
/// </summary>
internal static class ActiveSessionValidator
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var userId = SessionClaims.ReadUserId(context.Principal);
        var sessionId = SessionClaims.ReadSessionId(context.Principal);
        if (userId is null || sessionId is null)
        {
            context.Fail("The access token does not identify a session.");
            return;
        }

        var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        var active = await users.IsSessionActiveAsync(
            userId.Value,
            sessionId.Value,
            DateTime.UtcNow,
            context.HttpContext.RequestAborted);

        if (!active)
        {
            context.Fail("The session has ended.");
        }
    }
}

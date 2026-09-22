using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace VariableCompensation.Infrastructure.Auth;

internal static class SessionClaims
{
    public static long? ReadUserId(ClaimsPrincipal? principal)
    {
        var value = principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? principal?.FindFirstValue(ClaimTypes.Name)
                    ?? principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return long.TryParse(value, out var userId) ? userId : null;
    }

    /// <summary>
    /// Reads the <c>sid</c> claim under its own name as well as under the one the JWT handler may map it to.
    /// </summary>
    public static Guid? ReadSessionId(ClaimsPrincipal? principal)
    {
        var value = principal?.FindFirstValue(JwtRegisteredClaimNames.Sid)
                    ?? principal?.FindFirstValue(ClaimTypes.Sid);

        return Guid.TryParse(value, out var sessionId) ? sessionId : null;
    }
}

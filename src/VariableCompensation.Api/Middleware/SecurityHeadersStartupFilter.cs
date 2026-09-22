using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace VariableCompensation.Api.Middleware;

/// <summary>
/// Puts <see cref="SecurityHeadersMiddleware"/> in front of everything else, so no response can leave without
/// the headers, whichever part of the pipeline produced it.
/// </summary>
public sealed class SecurityHeadersStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        builder =>
        {
            builder.UseMiddleware<SecurityHeadersMiddleware>();
            next(builder);
        };
}

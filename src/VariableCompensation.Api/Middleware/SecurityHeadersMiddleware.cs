using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace VariableCompensation.Api.Middleware;

/// <summary>
/// The headers the API itself answers with. The frontend is served by Vercel, not by this app, so its policy
/// lives in vercel.json.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        // Added as the response starts: the exception handler clears everything written before it.
        context.Response.OnStarting(
            static state =>
            {
                var httpContext = (HttpContext)state;
                var headers = httpContext.Response.Headers;

                headers.XContentTypeOptions = "nosniff";
                headers.XFrameOptions = "DENY";
                headers["Referrer-Policy"] = "no-referrer";

                // Only the API itself. Swagger UI, which runs in development, needs its own scripts and styles.
                if (httpContext.Request.Path.StartsWithSegments("/api"))
                {
                    // Nothing the API returns is a document: it may not load anything or be framed.
                    headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";

                    // Answers carry salary data and access tokens, so no cache may keep them.
                    if (!headers.ContainsKey(HeaderNames.CacheControl))
                    {
                        headers.CacheControl = "no-store";
                    }
                }

                return Task.CompletedTask;
            },
            context);

        return this.next(context);
    }
}

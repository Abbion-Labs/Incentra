using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VariableCompensation.Domain;

namespace VariableCompensation.Host.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private const int ClientClosedRequest = 499;

    private readonly IProblemDetailsService problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger)
    {
        this.problemDetailsService = problemDetailsService;
        this.logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        var (status, errorCode) = exception switch
        {
            BadHttpRequestException badRequest => (badRequest.StatusCode, (string?)null),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, ErrorCodes.ConcurrencyConflict),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                (StatusCodes.Status409Conflict, ErrorCodes.DuplicateValue),
            _ => (StatusCodes.Status500InternalServerError, ErrorCodes.UnexpectedError),
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            this.logger.LogError(exception, "Unhandled exception for {Method} {Path} (trace {TraceId})", httpContext.Request.Method, httpContext.Request.Path, traceId);
        }
        else
        {
            this.logger.LogWarning(exception, "Request failed with {StatusCode} for {Method} {Path}: {ErrorCode} (trace {TraceId})", status, httpContext.Request.Method, httpContext.Request.Path, errorCode, traceId);
        }

        httpContext.Response.StatusCode = status;

        var problemDetailsContext = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = status },
        };

        if (errorCode is not null)
        {
            problemDetailsContext.ProblemDetails.Extensions["error"] = errorCode;
        }

        problemDetailsContext.ProblemDetails.Extensions["traceId"] = traceId;

        return await this.problemDetailsService.TryWriteAsync(problemDetailsContext);
    }
}

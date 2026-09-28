using Ghuri.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Ghuri.Api.ErrorHandling;

/// <summary>
/// The one place that catches exceptions nobody else caught, and turns
/// them into a proper ProblemDetails JSON response (RFC 9457) instead of
/// an HTML error page or a raw stack trace.
/// </summary>
/// <remarks>
/// Only two kinds of exception reach here:
/// - DomainException: an entity refused an invalid transition (e.g.
///   cancelling a completed booking). Blueprint: return HTTP 422 with the
///   stable code.
/// - Anything else: a genuine bug. Logged in full on the server, but the
///   client only gets a generic 500 - a stack trace would leak internals.
/// Expected failures (not found, not enough seats, bad input) never get
/// here - they travel as Result values and are mapped in ResultExtensions.
/// </remarks>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem;

        if (exception is DomainException domainException)
        {
            logger.LogWarning("Domain rule violated: {Code}", domainException.Code);
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = domainException.Message,
                Extensions = { ["code"] = domainException.Code }
            };
        }
        else
        {
            logger.LogError(exception, "Unhandled exception");
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Extensions = { ["code"] = "server_error" }
            };
        }

        httpContext.Response.StatusCode = problem.Status.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}

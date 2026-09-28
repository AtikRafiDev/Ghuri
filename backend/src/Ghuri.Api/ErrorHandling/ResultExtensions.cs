using Ghuri.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Ghuri.Api.ErrorHandling;

/// <summary>
/// Turns a use case's Result into an HTTP response. This is the ONLY
/// place that knows ErrorType.NotFound means 404 - handlers stay free of
/// HTTP, controllers stay one line long:
///     return (await sender.Send(command, ct)).ToActionResult();
/// </summary>
public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result) =>
        result.IsSuccess ? new NoContentResult() : ToProblem(result.Error);

    public static IActionResult ToActionResult<TValue>(this Result<TValue> result) =>
        result.IsSuccess ? new OkObjectResult(result.Value) : ToProblem(result.Error);

    /// <summary>
    /// Builds an RFC 9457 ProblemDetails body. The "code" extension is the
    /// stable machine-readable error code (e.g. "not_enough_seats") the
    /// frontend switches on - the blueprint's section 11 response shape.
    /// </summary>
    public static IActionResult ToProblem(Error error)
    {
        if (error is ValidationError validation)
        {
            // Per-field messages, shaped so the frontend can show each one
            // under its own input box.
            var validationProblem = new ValidationProblemDetails(
                validation.Errors.ToDictionary(e => e.Key, e => e.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = validation.Message,
                Extensions = { ["code"] = validation.Code }
            };
            return new ObjectResult(validationProblem) { StatusCode = validationProblem.Status };
        }

        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Message,
            Extensions = { ["code"] = error.Code }
        };
        return new ObjectResult(problem) { StatusCode = status };
    }
}

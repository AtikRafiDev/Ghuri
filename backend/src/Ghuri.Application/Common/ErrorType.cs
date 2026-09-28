namespace Ghuri.Application.Common;

/// <summary>
/// The KIND of failure. The Api layer turns each kind into an HTTP status
/// code (NotFound -> 404, Conflict -> 409...), so handlers never have to
/// know anything about HTTP.
/// </summary>
public enum ErrorType
{
    Failure,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden
}

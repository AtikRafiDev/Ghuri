namespace Ghuri.Application.Common;

/// <summary>
/// An EXPECTED failure, as a plain value (blueprint: "Result pattern -
/// expected failures are values, not exceptions"). "Not enough seats" is
/// not a crash - it's a normal business answer, so it's returned, not
/// thrown.
/// </summary>
/// <param name="Code">
/// Stable, machine-readable, snake_case - e.g. "not_enough_seats". The
/// frontend switches on this, so once shipped it must never change, even
/// if the Message wording does.
/// </param>
/// <param name="Message">Human-readable text, safe to show to the user.</param>
/// <param name="Type">Decides the HTTP status code in the Api layer.</param>
public record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Placeholder carried by a successful Result, so Error is never null.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    /// <summary>
    /// False (the default): TransactionBehavior rolls back everything the
    /// handler changed. True: the answer is still a failure, but the
    /// handler's changes are SAVED - for failures whose whole point is to
    /// record something. A wrong password must still save the
    /// failed-attempt count, or the account never locks; a reused refresh
    /// token must still save the revoked token family, or the thief keeps
    /// access.
    /// </summary>
    /// <example>
    /// <code>Error.Unauthorized("invalid_credentials", "Wrong phone or password.") with { CommitChanges = true }</code>
    /// </example>
    public bool CommitChanges { get; init; }

    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);
}

/// <summary>
/// Input validation failed on one or more fields. Errors is keyed by field
/// name ("Adults" -> ["Must be at least 1"]) - the exact shape ASP.NET
/// Core's ValidationProblemDetails uses, so the frontend can put each
/// message under the right input box (blueprint section 13.2).
/// </summary>
public sealed record ValidationError(IReadOnlyDictionary<string, string[]> Errors)
    : Error("validation_failed", "One or more validation errors occurred.", ErrorType.Validation);

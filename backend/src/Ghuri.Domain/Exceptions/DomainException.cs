namespace Ghuri.Domain.Exceptions;

/// <summary>
/// Thrown when an entity is asked to do something that breaks its own
/// rules - e.g. confirming a booking that's already cancelled. The Api
/// layer catches this and returns HTTP 422 with the Code, so the frontend
/// gets a stable, machine-readable reason instead of a generic error page.
/// </summary>
public sealed class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

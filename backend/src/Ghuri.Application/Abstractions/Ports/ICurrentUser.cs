using Ghuri.Domain.Enums;

namespace Ghuri.Application.Abstractions.Ports;

/// <summary>
/// "Who is making this request?" - answered without Application knowing
/// anything about HTTP, cookies or JWT. The Api layer implements it
/// (HttpCurrentUser) by reading the logged-in user from the access token.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Null when nobody is logged in, OR when there's no HTTP request at all
    /// (a background job). Audit columns treat null as "done by the system"
    /// - the same convention as BookingStatusHistory.ChangedBy.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>The roles written in the access token. Empty when nobody is logged in.</summary>
    IReadOnlyList<SystemRole> Roles { get; }

    /// <summary>
    /// For checks INSIDE handlers (blueprint section 8: "resource ownership
    /// is checked in handlers, not only by role") - e.g. staff may view any
    /// booking, a customer only their own.
    /// </summary>
    bool IsInRole(SystemRole role);
}

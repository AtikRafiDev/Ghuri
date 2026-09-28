namespace Ghuri.Application.Abstractions.Ports;

/// <summary>
/// "Who is making this request?" - answered without Application knowing
/// anything about HTTP, cookies or JWT. The Api layer implements it
/// (HttpCurrentUser) by reading the logged-in user from the request.
/// </summary>
/// <remarks>
/// Only UserId for now. Roles/IsInRole (blueprint section 9.3) arrive with
/// Day 2's login work - there is no login yet, so there are no roles to
/// read.
/// </remarks>
public interface ICurrentUser
{
    /// <summary>
    /// Null when nobody is logged in, OR when there's no HTTP request at all
    /// (a background job). Audit columns treat null as "done by the system"
    /// - the same convention as BookingStatusHistory.ChangedBy.
    /// </summary>
    Guid? UserId { get; }
}

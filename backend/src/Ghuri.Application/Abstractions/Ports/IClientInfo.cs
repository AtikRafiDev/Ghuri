namespace Ghuri.Application.Abstractions.Ports;

/// <summary>
/// Where the current request comes from. Recorded on every new session
/// (iam.RefreshTokens.CreatedByIp / UserAgent), so a user or support can
/// later see "logged in from Chrome on Windows, 103.x.x.x".
/// </summary>
/// <remarks>
/// A port, like ICurrentUser: only the Api knows about HTTP, so only the
/// Api can answer. Keeping it out of the commands means a client can't
/// simply type a fake IP address into the request body.
/// </remarks>
public interface IClientInfo
{
    string? IpAddress { get; }
    string? UserAgent { get; }
}

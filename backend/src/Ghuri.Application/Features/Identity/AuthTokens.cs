namespace Ghuri.Application.Features.Identity;

/// <summary>
/// A new session: what Login, RegisterCustomer, RefreshSession and
/// ChangePassword hand back to the Api.
/// </summary>
/// <remarks>
/// The Api splits it up: AccessToken goes in the JSON response (the
/// frontend keeps it in memory), RefreshToken goes ONLY into an HttpOnly
/// cookie - JavaScript can never read it, so a malicious script can't
/// steal the long-lived "stay logged in" token (blueprint section 8).
/// </remarks>
public sealed record AuthTokens(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

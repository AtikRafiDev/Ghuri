using Ghuri.Application.Abstractions.Ports;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace Ghuri.Application.Features.Identity;

/// <summary>
/// Starts a session: one access token + one refresh token (stored as a
/// hash). Shared by every command that logs someone in - Login,
/// RegisterCustomer, RefreshSession, ChangePassword - so "what a session
/// is" is written exactly once.
/// </summary>
internal sealed class SessionIssuer(
    ITokenService tokens,
    IRefreshTokenRepository refreshTokens,
    IClientInfo client,
    IOptions<AuthOptions> options)
{
    /// <param name="user">Loaded WITH roles, so they end up in the access token.</param>
    /// <param name="familyId">
    /// A NEW id for a fresh login; the old token's FamilyId when rotating,
    /// so every token of one login stays in one family (for reuse detection).
    /// </param>
    /// <param name="nowUtc">The current time, from the caller's TimeProvider.</param>
    public AuthTokens Issue(User user, Guid familyId, DateTime nowUtc)
    {
        var accessToken = tokens.CreateAccessToken(user);
        var refreshToken = tokens.CreateOpaqueToken();

        // "Sliding" lifetime: every refresh gives another 30 days, so a
        // customer who visits monthly never has to log in again - but a
        // stolen token dies within 30 days of the thief's last use.
        var refreshExpiresAtUtc = nowUtc.AddDays(options.Value.RefreshTokenDays);

        refreshTokens.Add(RefreshToken.Issue(
            user.Id, refreshToken.Hash, familyId, nowUtc, refreshExpiresAtUtc, client.IpAddress, client.UserAgent));

        return new AuthTokens(accessToken.Value, accessToken.ExpiresAtUtc, refreshToken.Value, refreshExpiresAtUtc);
    }
}

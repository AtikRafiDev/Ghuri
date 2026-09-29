using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Identity.Commands.RefreshSession;

/// <summary>Rotation with theft detection (blueprint section 8: "reuse revokes the token family").</summary>
internal sealed class RefreshSessionHandler(
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    ITokenService tokens,
    SessionIssuer sessions,
    TimeProvider clock) : ICommandHandler<RefreshSessionCommand, AuthTokens>
{
    /// <summary>
    /// Two browser tabs opening at the same moment both send the SAME
    /// cookie; one wins the rotation, the other arrives a moment later with
    /// the now-spent token. That's not theft, so reuse within this window
    /// is refused WITHOUT revoking the family. After it, reuse = theft.
    /// </summary>
    private static readonly TimeSpan ReuseGracePeriod = TimeSpan.FromSeconds(30);

    public async ValueTask<Result<AuthTokens>> Handle(RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var current = await refreshTokens.GetByHashAsync(tokens.HashOpaqueToken(command.RefreshToken), cancellationToken);
        if (current is null)
            return IdentityErrors.SessionExpired;

        if (current.IsRotated)
        {
            // This token was already exchanged once, yet here it is again:
            // two parties hold a copy - the real user and a thief - and we
            // can't tell which is which. So end the whole login for both.
            if (current.RevokedAtUtc is { } rotatedAtUtc && nowUtc - rotatedAtUtc > ReuseGracePeriod)
                await refreshTokens.RevokeFamilyAsync(current.FamilyId, nowUtc, cancellationToken);

            return IdentityErrors.SessionExpired; // CommitChanges keeps the revoke
        }

        if (!current.IsActive(nowUtc))
            return IdentityErrors.SessionExpired; // logged out, or older than 30 days

        var user = await users.GetByIdAsync(current.UserId, cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
        {
            current.Revoke(nowUtc); // a disabled account's sessions end here
            return IdentityErrors.SessionExpired;
        }

        // Same family as before: this is the SAME login, continued.
        var session = sessions.Issue(user, current.FamilyId, nowUtc);
        current.ReplaceWith(tokens.HashOpaqueToken(session.RefreshToken), nowUtc);
        return session;
    }
}

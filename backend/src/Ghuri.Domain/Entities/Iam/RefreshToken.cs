using Ghuri.Domain.Exceptions;

namespace Ghuri.Domain.Entities.Iam;

/// <summary>
/// One rotating refresh token. The blueprint calls this a "high-volume
/// log" table (BIGINT IDENTITY id, not a Guid via BaseEntity) - there can
/// be many rows per user over time, and nothing else ever needs to
/// reference a specific token by a stable business id.
/// </summary>
/// <remarks>
/// Only TokenHash is ever stored - never the raw token - so a stolen
/// database backup can't be used to log in as anyone.
/// Rotation: every refresh spends the old token and issues a new one in
/// the same family. If a SPENT token ever comes back (IsRotated), two
/// parties hold it - the real user and a thief - and the handler revokes
/// the whole family, logging both out (blueprint section 8).
/// </remarks>
public sealed class RefreshToken
{
    public long Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>All rotations issued from one original login share this id.</summary>
    public Guid FamilyId { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    /// <summary>Set when this token is rotated into a new one; a later reuse of this hash revokes the whole family.</summary>
    public string? ReplacedByHash { get; private set; }

    public string? CreatedByIp { get; private set; }
    public string? UserAgent { get; private set; }

    private RefreshToken()
    {
    }

    public static RefreshToken Issue(
        Guid userId, string tokenHash, Guid familyId, DateTime nowUtc, DateTime expiresAtUtc,
        string? createdByIp, string? userAgent) =>
        new()
        {
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = familyId,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = expiresAtUtc,
            CreatedByIp = createdByIp,
            UserAgent = userAgent
        };

    /// <summary>Usable right now: not revoked (logout, rotation, reuse) and not past its deadline.</summary>
    public bool IsActive(DateTime nowUtc) => RevokedAtUtc is null && nowUtc < ExpiresAtUtc;

    /// <summary>
    /// Already swapped for a newer token. Presenting it again is REUSE -
    /// the signal of a stolen token. A token revoked by logout is not
    /// rotated, so logging out and pressing Back is not mistaken for theft.
    /// </summary>
    public bool IsRotated => ReplacedByHash is not null;

    /// <summary>Ends this token (logout, password change, reuse detected). Revoking twice keeps the first time.</summary>
    public void Revoke(DateTime nowUtc) => RevokedAtUtc ??= nowUtc;

    /// <summary>
    /// Rotation: this token is spent and newTokenHash takes over. Only an
    /// active token can be rotated - the handler must spot a revoked one
    /// (and treat a rotated one as reuse) before getting here.
    /// </summary>
    public void ReplaceWith(string newTokenHash, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newTokenHash);
        if (!IsActive(nowUtc))
            throw new DomainException("refresh_token_inactive", "Your session has ended. Please log in again.");

        ReplacedByHash = newTokenHash;
        RevokedAtUtc = nowUtc;
    }
}

namespace Ghuri.Domain.Entities.Iam;

/// <summary>
/// One rotating refresh token. The blueprint calls this a "high-volume
/// log" table (BIGINT IDENTITY id, not a Guid via BaseEntity) - there can
/// be many rows per user over time, and nothing else ever needs to
/// reference a specific token by a stable business id.
/// </summary>
/// <remarks>
/// Only TokenHash is ever stored - never the raw token - so a stolen
/// database backup can't be used to log in as anyone. Rotation and
/// "reuse revokes the whole family" logic belongs to Day 2's Identity
/// feature.
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
}

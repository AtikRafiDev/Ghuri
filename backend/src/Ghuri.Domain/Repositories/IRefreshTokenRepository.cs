using Ghuri.Domain.Entities.Iam;

namespace Ghuri.Domain.Repositories;

/// <summary>Sessions: one row per refresh token ever issued (blueprint section 7.1).</summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// tokenHash = SHA-256 of the raw token from the cookie. The raw token
    /// is never stored, so it is never searched for either.
    /// </summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);

    void Add(RefreshToken token);

    /// <summary>Revokes every still-active token of ONE login - on logout, or when reuse is detected.</summary>
    Task RevokeFamilyAsync(Guid familyId, DateTime nowUtc, CancellationToken cancellationToken);

    /// <summary>Revokes every still-active token of a user on EVERY device - after a password change or reset.</summary>
    Task RevokeAllForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken);
}

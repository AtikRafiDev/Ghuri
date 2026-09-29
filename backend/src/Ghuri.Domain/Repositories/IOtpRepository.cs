using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;

namespace Ghuri.Domain.Repositories;

/// <summary>One-time codes and password-reset tokens (blueprint section 7.1).</summary>
public interface IOtpRepository
{
    /// <summary>
    /// The NEWEST code sent to this destination for this purpose. Only the
    /// newest one is ever checked, so asking for a new reset link makes
    /// every earlier link useless automatically.
    /// </summary>
    Task<OtpCode?> GetLatestAsync(string destination, OtpPurpose purpose, CancellationToken cancellationToken);

    /// <summary>
    /// How many codes were sent here since sinceUtc - lets the handler cap
    /// how often someone can make us email/SMS one address.
    /// </summary>
    Task<int> CountSinceAsync(string destination, OtpPurpose purpose, DateTime sinceUtc, CancellationToken cancellationToken);

    void Add(OtpCode code);
}

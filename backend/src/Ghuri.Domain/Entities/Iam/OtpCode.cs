using Ghuri.Domain.Enums;

namespace Ghuri.Domain.Entities.Iam;

/// <summary>
/// A one-time code sent to a phone or email for login, phone verification,
/// or password reset. Same "high-volume log" shape as RefreshToken -
/// BIGINT IDENTITY id, not a Guid.
/// </summary>
public sealed class OtpCode
{
    public long Id { get; private set; }

    /// <summary>The phone number or email address the code was sent to.</summary>
    public string Destination { get; private set; } = string.Empty;

    public OtpPurpose Purpose { get; private set; }

    /// <summary>SHA-256 of the 6-digit code - never the raw code.</summary>
    public string CodeHash { get; private set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; private set; }
    public byte Attempts { get; private set; }
    public DateTime? ConsumedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private OtpCode()
    {
    }

    public static OtpCode Create(string destination, OtpPurpose purpose, string codeHash, DateTime nowUtc, DateTime expiresAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        ArgumentException.ThrowIfNullOrWhiteSpace(codeHash);

        return new OtpCode
        {
            Destination = destination,
            Purpose = purpose,
            CodeHash = codeHash,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = expiresAtUtc,
            Attempts = 0
        };
    }
}

using Ghuri.Domain.Enums;
using Ghuri.Domain.Exceptions;

namespace Ghuri.Domain.Entities.Iam;

/// <summary>
/// A one-time secret sent to a phone or email for login, phone
/// verification, or password reset. Same "high-volume log" shape as
/// RefreshToken - BIGINT IDENTITY id, not a Guid.
/// </summary>
/// <remarks>
/// Two kinds of secret share this table, with different lifetimes:
/// <list type="bullet">
/// <item>Login / VerifyPhone: a 6-digit code typed in from an SMS -
/// valid 5 minutes (blueprint section 15, Otp.ValidityMinutes).</item>
/// <item>ResetPassword: a long random token inside an emailed link -
/// valid 30 minutes. 5 minutes is too short for someone to notice the
/// email and click; the token is unguessable, so the extra time costs
/// nothing in security.</item>
/// </list>
/// The caller passes expiresAtUtc, so each flow picks its own lifetime.
/// Either way Attempts caps wrong guesses at 5 (a CHECK constraint backs it).
/// </remarks>
public sealed class OtpCode
{
    /// <summary>
    /// Wrong guesses allowed per code. The database constraint
    /// CK_OtpCodes_Attempts is built from this same constant, so the two
    /// can never disagree.
    /// </summary>
    public const byte MaxAttempts = 5;

    public long Id { get; private set; }

    /// <summary>The phone number or email address the code was sent to.</summary>
    public string Destination { get; private set; } = string.Empty;

    public OtpPurpose Purpose { get; private set; }

    /// <summary>SHA-256 of the code or reset token - never the raw value, so a leaked backup can't reset anyone's password.</summary>
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

    /// <summary>Past its deadline. Like a lock, nothing "disables" the code - time simply passes ExpiresAtUtc.</summary>
    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;

    /// <summary>
    /// Can this code still be accepted? Not used yet, not expired, and not
    /// burned by too many wrong guesses. The handler checks this BEFORE
    /// comparing the submitted value.
    /// </summary>
    public bool IsUsable(DateTime nowUtc) => ConsumedAtUtc is null && Attempts < MaxAttempts && !IsExpired(nowUtc);

    /// <summary>
    /// One wrong guess. At MaxAttempts the code is dead, even if the right
    /// value arrives later - otherwise a 6-digit SMS code (only a million
    /// possibilities) could simply be guessed.
    /// </summary>
    public void RecordWrongAttempt()
    {
        if (Attempts < MaxAttempts) // never above the database's CHECK constraint
            Attempts++;
    }

    /// <summary>Marks the code used, so the same link or code can never work twice.</summary>
    public void Consume(DateTime nowUtc)
    {
        if (!IsUsable(nowUtc))
            throw new DomainException("otp_not_usable", "This code has expired or was already used.");

        ConsumedAtUtc = nowUtc;
    }
}

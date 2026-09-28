using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Entities.Iam;

/// <summary>
/// Every person who logs in - customers and admin staff share this one
/// table (blueprint: iam.Users [A]). [A] means Infrastructure adds
/// CreatedAtUtc/CreatedBy/UpdatedAtUtc/UpdatedBy as shadow columns - see
/// IAuditable.
/// </summary>
/// <remarks>
/// Only the Create factory is implemented here for now. Real behaviour -
/// ConfirmPhone, ChangePassword (which must also generate a new
/// SecurityStamp and revoke refresh tokens), lockout after failed logins -
/// belongs to Day 2's Identity feature, built alongside the actual login
/// commands so it can be tested against something real instead of
/// guessed at in isolation.
/// </remarks>
public sealed class User : AggregateRoot, IAuditable
{
    public string FullName { get; private set; } = string.Empty;

    /// <summary>Optional for customers, required for staff (enforced by the command handler, not here).</summary>
    public string? Email { get; private set; }

    /// <summary>Upper-cased copy of Email, used for case-insensitive lookups.</summary>
    public string? NormalizedEmail { get; private set; }

    public PhoneNumber PhoneNumber { get; private set; } = null!;

    /// <summary>Null for accounts that only ever sign in with Google.</summary>
    public string? PasswordHash { get; private set; }

    /// <summary>Changes whenever the password changes; existing refresh tokens become invalid when it does.</summary>
    public string SecurityStamp { get; private set; } = string.Empty;

    public bool EmailConfirmed { get; private set; }
    public bool PhoneConfirmed { get; private set; }
    public string? GoogleSubject { get; private set; }

    /// <summary>
    /// FK to ops.FileObjects - that table doesn't exist yet (it's the last
    /// schema we'll build), so this is a plain column for now with no EF
    /// Core relationship configured. The real FK constraint gets added
    /// when we reach the ops schema.
    /// </summary>
    public Guid? AvatarFileId { get; private set; }

    public UserStatus Status { get; private set; }
    public byte AccessFailedCount { get; private set; }
    public DateTime? LockoutEndUtc { get; private set; }
    public DateTime? LastLoginUtc { get; private set; }

    private User()
    {
    }

    /// <summary>
    /// passwordHash is expected ALREADY hashed - hashing itself is done by
    /// Infrastructure's IPasswordHasher (a port, per the blueprint), never
    /// by Domain, which has no package references and so can't use a
    /// hashing library.
    /// </summary>
    public static User Create(string fullName, PhoneNumber phoneNumber, string? email, string? passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        return new User
        {
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Email = email,
            NormalizedEmail = email?.ToUpperInvariant(),
            PasswordHash = passwordHash,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            Status = UserStatus.Active,
            EmailConfirmed = false,
            PhoneConfirmed = false,
            AccessFailedCount = 0
        };
    }
}

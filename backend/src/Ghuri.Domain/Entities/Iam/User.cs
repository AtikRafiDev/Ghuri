using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Exceptions;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Entities.Iam;

/// <summary>
/// Every person who logs in - customers and admin staff share this one
/// table (blueprint: iam.Users [A]). [A] means Infrastructure adds
/// CreatedAtUtc/CreatedBy/UpdatedAtUtc/UpdatedBy as shadow columns - see
/// IAuditable.
/// </summary>
/// <remarks>
/// The login rules live HERE, not in the command handlers - so Login, a
/// future Google login, or an admin tool all lock/unlock accounts the
/// exact same way (blueprint section 6: "business rules live on the
/// aggregates, so every entry point behaves the same"). The limits
/// themselves (5 attempts, 15 minutes) are passed in by the caller: they
/// live in appsettings.json, which Domain cannot read.
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

    private readonly List<UserRole> _roles = [];

    /// <summary>
    /// Part of the User aggregate: a role assignment means nothing without
    /// its user, so it's only ever added through AssignRole and saved
    /// together with the user - it has no repository of its own.
    /// </summary>
    public IReadOnlyList<UserRole> Roles => _roles.AsReadOnly();

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
            SecurityStamp = NewSecurityStamp(),
            Status = UserStatus.Active,
            EmailConfirmed = false,
            PhoneConfirmed = false,
            AccessFailedCount = 0
        };
    }

    /// <summary>
    /// True while a lock from too many wrong passwords is still running.
    /// Nothing ever "unlocks" the account: once nowUtc passes
    /// LockoutEndUtc, this simply starts returning false.
    /// </summary>
    /// <remarks>Only this temporary lock - an admin-disabled account (Status) is a separate check.</remarks>
    public bool IsLockedOut(DateTime nowUtc) => LockoutEndUtc > nowUtc;

    /// <summary>
    /// Counts one wrong password. On reaching maxFailedAttempts, locks the
    /// account until nowUtc + lockoutDuration and restarts the count at 0,
    /// so after the lock ends the user gets a full set of attempts again.
    /// </summary>
    /// <remarks>
    /// Does nothing while already locked: during a lock the handler refuses
    /// before even checking the password, so there is no "wrong password"
    /// to count - and the lock stays exactly 15 minutes, as the user was told.
    /// </remarks>
    public void RecordFailedLogin(DateTime nowUtc, int maxFailedAttempts, TimeSpan lockoutDuration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxFailedAttempts, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxFailedAttempts, byte.MaxValue); // AccessFailedCount is a TINYINT
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lockoutDuration, TimeSpan.Zero);

        if (IsLockedOut(nowUtc))
            return;

        AccessFailedCount++;
        if (AccessFailedCount >= maxFailedAttempts)
        {
            LockoutEndUtc = nowUtc + lockoutDuration; // save the deadline, not a timer
            AccessFailedCount = 0;
        }
    }

    /// <summary>A correct password: forget earlier failures and remember when they logged in.</summary>
    /// <remarks>
    /// Refuses while locked - even the right password must wait out the
    /// lock, or an attacker who guessed right on attempt 6 would get in.
    /// The handler checks IsLockedOut first; this is the safety net.
    /// </remarks>
    public void RecordSuccessfulLogin(DateTime nowUtc)
    {
        if (IsLockedOut(nowUtc))
            throw new DomainException("account_locked", "Too many wrong passwords. Try again later.");

        AccessFailedCount = 0;
        LockoutEndUtc = null;
        LastLoginUtc = nowUtc;
    }

    /// <summary>
    /// Sets a new, ALREADY HASHED password - for reset, change, and the
    /// Super Admin's first password alike.
    /// </summary>
    /// <remarks>
    /// Always issues a new SecurityStamp, marking everything from before as
    /// "issued under the old password". Also clears a running lock: a reset
    /// proves the person owns the email inbox, so making them wait out a
    /// lock caused by someone else's guesses would only punish the real owner.
    /// </remarks>
    public void SetPassword(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        PasswordHash = passwordHash;
        SecurityStamp = NewSecurityStamp();
        AccessFailedCount = 0;
        LockoutEndUtc = null;
    }

    /// <summary>Gives the user a role. Assigning a role they already have does nothing.</summary>
    public void AssignRole(SystemRole role, DateTime nowUtc)
    {
        if (HasRole(role))
            return;

        _roles.Add(UserRole.Create(Id, (byte)role, nowUtc));
    }

    public bool HasRole(SystemRole role) => _roles.Exists(r => r.RoleId == (byte)role);

    private static string NewSecurityStamp() => Guid.NewGuid().ToString("N");
}

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
        email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

        return new User
        {
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Email = email,
            NormalizedEmail = email is null ? null : NormalizeEmail(email),
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

    /// <summary>
    /// The user's own name and email (Day 11: UpdateProfile). The phone
    /// number can't change here: it's how they log in, and changing it needs
    /// an SMS code (Phase 2: OTP).
    /// </summary>
    /// <remarks>A different email is not confirmed yet - EmailConfirmed goes back to false.</remarks>
    public void UpdateProfile(string fullName, string? email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

        if (!EmailEquals(Email, email))
            EmailConfirmed = false;

        FullName = fullName.Trim();
        Email = email;
        NormalizedEmail = email is null ? null : NormalizeEmail(email);
    }

    /// <summary>Same address, ignoring case and spaces (NormalizeEmail) - or both empty.</summary>
    public static bool EmailEquals(string? a, string? b) =>
        a is null || b is null ? a is null && b is null : NormalizeEmail(a) == NormalizeEmail(b);

    /// <summary>Gives the user a role. Assigning a role they already have does nothing.</summary>
    public void AssignRole(SystemRole role, DateTime nowUtc)
    {
        if (HasRole(role))
            return;

        _roles.Add(UserRole.Create(Id, (byte)role, nowUtc));
    }

    public bool HasRole(SystemRole role) => _roles.Exists(r => r.RoleId == (byte)role);

    /// <summary>
    /// The roles the Super Admin can give from the admin panel's Staff page.
    /// SuperAdmin itself is never handed out there - it comes only from the
    /// seed command - and Customer is what people get by signing up themselves.
    /// </summary>
    public static readonly IReadOnlyList<SystemRole> AssignableStaffRoles = [SystemRole.Manager, SystemRole.Sales, SystemRole.Accounts];

    /// <summary>Has any admin-panel role (SuperAdmin, Manager, Sales or Accounts).</summary>
    public bool IsStaff => HasRole(SystemRole.SuperAdmin) || AssignableStaffRoles.Any(HasRole);

    /// <summary>
    /// A staff account made by the Super Admin (Staff page). No password:
    /// the person sets their own from the emailed link, so the admin never
    /// knows it. Staff log in and reset passwords by email, so it's required.
    /// </summary>
    public static User CreateStaff(string fullName, PhoneNumber phoneNumber, string email, SystemRole role, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        EnsureAssignableStaffRole(role);

        var user = Create(fullName.Trim(), phoneNumber, email, passwordHash: null);
        user.AssignRole(role, nowUtc);
        return user;
    }

    /// <summary>
    /// Makes this staff member Manager, Sales or Accounts - replacing the one
    /// they had. The Super Admin's own account can't be changed this way.
    /// </summary>
    /// <remarks>Takes effect at their next login or token refresh (at most 15 minutes, the access token's life).</remarks>
    public void ChangeStaffRole(SystemRole role, DateTime nowUtc)
    {
        EnsureAssignableStaffRole(role);
        EnsureEditableStaff();

        // EF Core deletes a UserRole row removed from this list (required relationship).
        _roles.RemoveAll(r => r.RoleId != (byte)role && AssignableStaffRoles.Contains((SystemRole)r.RoleId));
        AssignRole(role, nowUtc);
    }

    /// <summary>
    /// Stops this staff member logging in. Login and token refresh already
    /// refuse a non-Active account; the handler also ends their sessions.
    /// Nothing is deleted - their name stays on the bookings and refunds they handled.
    /// </summary>
    public void Disable()
    {
        EnsureEditableStaff();
        Status = UserStatus.Disabled;
    }

    /// <summary>Lets a disabled staff member log in again.</summary>
    public void Enable()
    {
        EnsureEditableStaff();
        Status = UserStatus.Active;
    }

    private static void EnsureAssignableStaffRole(SystemRole role)
    {
        if (!AssignableStaffRoles.Contains(role))
            throw new DomainException("role_not_assignable", "Staff can only be made Manager, Sales or Accounts.");
    }

    // The safety net under the handlers' own checks: only staff accounts are
    // managed here, and never the Super Admin - nobody may lock the owner out.
    private void EnsureEditableStaff()
    {
        if (HasRole(SystemRole.SuperAdmin))
            throw new DomainException("super_admin_protected", "The Super Admin account can't be changed here.");
        if (!IsStaff)
            throw new DomainException("not_staff", "This account is not a staff account.");
    }

    /// <summary>
    /// THE one rule for comparing emails: " Rahim@Mail.com" and
    /// "rahim@mail.com" are the same address. Used when saving
    /// NormalizedEmail AND when searching by it - two different rules
    /// would make users impossible to find.
    /// </summary>
    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    private static string NewSecurityStamp() => Guid.NewGuid().ToString("N");
}

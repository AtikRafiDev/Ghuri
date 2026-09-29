using Ghuri.Application.Common;

namespace Ghuri.Application.Features.Identity;

/// <summary>
/// Every expected failure of the Identity feature, in one place. The Code
/// is what the frontend switches on, so it must never change once shipped.
/// </summary>
/// <remarks>
/// CommitChanges = true is set on errors that may come right AFTER the
/// handler recorded something that must survive the failure - a failed-
/// attempt count, a lock, a revoked session. Without it, TransactionBehavior
/// would roll that record back and the protection would silently never
/// happen. When nothing was changed, committing is harmless.
/// Public so the Api can reuse an error for a purely HTTP-level case
/// (e.g. "no refresh cookie at all") instead of inventing a second copy.
/// </remarks>
public static class IdentityErrors
{
    // Login / change password. Unknown user and wrong password give the
    // SAME answer, so the login form never tells a stranger which accounts exist.
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("invalid_credentials", "Wrong phone/email or password.") with { CommitChanges = true };

    public static readonly Error AccountLocked =
        Error.Unauthorized("account_locked", "Too many wrong passwords. Please try again in a few minutes.") with { CommitChanges = true };

    public static readonly Error AccountDisabled =
        Error.Unauthorized("account_disabled", "This account has been disabled. Please contact support.");

    public static readonly Error CurrentPasswordWrong =
        Error.Failure("current_password_wrong", "Your current password is not correct.") with { CommitChanges = true };

    // Sessions. Every refresh problem gets one answer: "log in again".
    public static readonly Error SessionExpired =
        Error.Unauthorized("session_expired", "Your session has ended. Please log in again.") with { CommitChanges = true };

    public static readonly Error NotAuthenticated =
        Error.Unauthorized("not_authenticated", "Please log in.");

    // Password reset. Unknown email, expired, used or wrong token: all one
    // answer, for the same reason as InvalidCredentials.
    public static readonly Error ResetLinkInvalid =
        Error.Failure("reset_link_invalid", "This reset link is invalid or has expired. Please request a new one.") with { CommitChanges = true };

    // Registration.
    public static readonly Error PhoneTaken =
        Error.Conflict("phone_taken", "This phone number is already registered.");

    public static readonly Error EmailTaken =
        Error.Conflict("email_taken", "This email is already registered.");
}

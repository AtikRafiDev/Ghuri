namespace Ghuri.Application.Features.Identity;

/// <summary>
/// The login rules' NUMBERS, read from the "Auth" section of
/// appsettings.json. The rules themselves live in the Domain entities
/// (User, OtpCode); handlers read these numbers and pass them in.
/// </summary>
/// <remarks>
/// No default values here on purpose: appsettings.json is the ONE place
/// the numbers are written, and startup validation (AddApplication) stops
/// the app if any is missing - so there's never a second, forgotten copy.
/// </remarks>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Wrong passwords in a row before the account locks.</summary>
    public int MaxFailedLogins { get; init; }

    /// <summary>How long a lock lasts.</summary>
    public int LockoutMinutes { get; init; }

    /// <summary>How long a refresh token (the "stay logged in" cookie) works.</summary>
    public int RefreshTokenDays { get; init; }

    /// <summary>How long a password-reset link works.</summary>
    public int PasswordResetLinkMinutes { get; init; }

    /// <summary>How long the "set your password" link in a new staff account's welcome email works.</summary>
    public int StaffInviteLinkHours { get; init; }

    /// <summary>Most reset emails one address can trigger per hour - stops someone flooding a stranger's inbox.</summary>
    public int MaxResetEmailsPerHour { get; init; }

    /// <summary>The frontend page the reset email links to, e.g. http://localhost:5173/reset-password.</summary>
    public string PasswordResetUrl { get; init; } = string.Empty;
}

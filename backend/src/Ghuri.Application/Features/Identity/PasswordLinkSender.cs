using System.Net;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace Ghuri.Application.Features.Identity;

/// <summary>
/// Emails a one-time "set your password" link - for Forgot password and for
/// a new staff account's welcome email alike. Both open the same page
/// (/reset-password) and are checked by the same ResetPassword command.
/// </summary>
/// <remarks>
/// The token's HASH goes to iam.OtpCodes; the token itself only into the
/// email. A new link makes older ones useless, because ResetPassword only
/// ever checks the newest. The email is sent inside the command's
/// transaction: if sending fails, nothing is saved and the caller sees the
/// error. (Not through the outbox - that would store the token in plain text.)
/// </remarks>
internal sealed class PasswordLinkSender(
    IOtpRepository otps,
    ITokenService tokens,
    IEmailSender emails,
    IOptions<AuthOptions> options)
{
    /// <summary>Forgot password: a short-lived link (Auth:PasswordResetLinkMinutes).</summary>
    public Task SendResetLinkAsync(User user, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var minutes = options.Value.PasswordResetLinkMinutes;
        return SendAsync(user, nowUtc, TimeSpan.FromMinutes(minutes), "Reset your Ghuri password", link =>
            $"""
            <p>Hi {WebUtility.HtmlEncode(user.FullName)},</p>
            <p><a href="{WebUtility.HtmlEncode(link)}">Set a new password</a> - this link works once, for {minutes} minutes.</p>
            <p>If you didn't ask for this, ignore this email. Your password stays the same.</p>
            """, cancellationToken);
    }

    /// <summary>
    /// A new staff account: a longer-lived link (Auth:StaffInviteLinkHours) -
    /// the person may not read the email for a day or two.
    /// </summary>
    public Task SendStaffInviteAsync(User user, string roleName, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var hours = options.Value.StaffInviteLinkHours;
        return SendAsync(user, nowUtc, TimeSpan.FromHours(hours), "Your Ghuri staff account", link =>
            $"""
            <p>Hi {WebUtility.HtmlEncode(user.FullName)},</p>
            <p>A Ghuri staff account has been made for you, with the role <b>{WebUtility.HtmlEncode(roleName)}</b>.</p>
            <p><a href="{WebUtility.HtmlEncode(link)}">Set your password</a> - this link works once, for {hours} hours.
            Then log in with this email or your mobile number.</p>
            <p>If you weren't expecting this, ignore this email.</p>
            """, cancellationToken);
    }

    private async Task SendAsync(
        User user, DateTime nowUtc, TimeSpan validFor, string subject, Func<string, string> body, CancellationToken cancellationToken)
    {
        if (user.Email is null)
            throw new InvalidOperationException("A password link needs an email address."); // callers check first

        var token = tokens.CreateOpaqueToken();
        otps.Add(OtpCode.Create(
            User.NormalizeEmail(user.Email), OtpPurpose.ResetPassword, token.Hash, nowUtc, nowUtc + validFor));

        var link = $"{options.Value.PasswordResetUrl}?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token.Value)}";
        await emails.SendAsync(new EmailMessage(user.Email, subject, body(link)), cancellationToken);
    }
}

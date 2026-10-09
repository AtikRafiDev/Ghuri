using System.Net;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
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
        var expires = $"{minutes} minute{(minutes == 1 ? "" : "s")}";
        return SendAsync(user, nowUtc, TimeSpan.FromMinutes(minutes), "Reset your Ghuri password", link => EmailLayout.Page(
            "Reset your password",
            $"Choose a new password for your Ghuri account - the link works for {expires}.",
            EmailLayout.Paragraph($"Hi {WebUtility.HtmlEncode(user.FullName)},") +
            EmailLayout.Paragraph("We got a request to reset the password for your Ghuri account. Click the button below to choose a new one.") +
            EmailLayout.Button("Set a new password", link) +
            EmailLayout.Note($"This link works <strong>once</strong> and expires in <strong>{expires}</strong>.") +
            EmailLayout.Note("Didn't ask for this? Just ignore this email - your password stays the same.")), cancellationToken);
    }

    /// <summary>
    /// A new staff account: a longer-lived link (Auth:StaffInviteLinkHours) -
    /// the person may not read the email for a day or two.
    /// </summary>
    public Task SendStaffInviteAsync(User user, string roleName, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var hours = options.Value.StaffInviteLinkHours;
        var expires = $"{hours} hour{(hours == 1 ? "" : "s")}";
        return SendAsync(user, nowUtc, TimeSpan.FromHours(hours), "Your Ghuri staff account", link => EmailLayout.Page(
            "Welcome to the Ghuri team",
            $"Set your password to start using your Ghuri staff account - the link works for {expires}.",
            EmailLayout.Paragraph($"Hi {WebUtility.HtmlEncode(user.FullName)},") +
            EmailLayout.Paragraph($"A Ghuri staff account has been made for you, with the role <strong>{WebUtility.HtmlEncode(roleName)}</strong>. Set a password to get started.") +
            EmailLayout.Button("Set your password", link) +
            EmailLayout.Paragraph("After that, log in with this email address or your mobile number.") +
            EmailLayout.Note($"This link works <strong>once</strong> and expires in <strong>{expires}</strong>.") +
            EmailLayout.Note("Weren't expecting this? Just ignore this email.")), cancellationToken);
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

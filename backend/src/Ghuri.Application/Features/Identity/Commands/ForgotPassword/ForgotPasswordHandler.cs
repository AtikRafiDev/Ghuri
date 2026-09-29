using System.Net;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace Ghuri.Application.Features.Identity.Commands.ForgotPassword;

/// <summary>
/// Creates a one-time reset token, stores only its hash (iam.OtpCodes),
/// and emails the link containing the real token.
/// </summary>
/// <remarks>
/// The answer is ALWAYS success - for an unknown email, a disabled
/// account, or when the hourly limit is reached. Any other answer would
/// let a stranger test which emails have an account here.
/// </remarks>
internal sealed class ForgotPasswordHandler(
    IUserRepository users,
    IOtpRepository otps,
    ITokenService tokens,
    IEmailSender emails,
    IOptions<AuthOptions> options,
    TimeProvider clock) : ICommandHandler<ForgotPasswordCommand>
{
    public async ValueTask<Result> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null || user.Email is null || user.Status != UserStatus.Active)
            return Result.Success();

        var settings = options.Value;
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var destination = User.NormalizeEmail(user.Email);

        // Quietly skip if this address already got too many links this
        // hour - stops someone flooding a stranger's inbox.
        var sentLastHour = await otps.CountSinceAsync(
            destination, OtpPurpose.ResetPassword, nowUtc.AddHours(-1), cancellationToken);
        if (sentLastHour >= settings.MaxResetEmailsPerHour)
            return Result.Success();

        // The token's HASH goes to the database; the token itself only
        // into the email. Requesting a new link makes older ones useless,
        // because ResetPassword only ever checks the newest.
        var token = tokens.CreateOpaqueToken();
        otps.Add(OtpCode.Create(
            destination, OtpPurpose.ResetPassword, token.Hash, nowUtc,
            nowUtc.AddMinutes(settings.PasswordResetLinkMinutes)));

        var link = $"{settings.PasswordResetUrl}?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token.Value)}";
        await emails.SendAsync(
            new EmailMessage(user.Email, "Reset your Ghuri password",
                ResetEmailBody(user.FullName, link, settings.PasswordResetLinkMinutes)),
            cancellationToken);

        return Result.Success();
    }

    // HtmlEncode: the name is typed by the user - encoding stops a name
    // like "<script>..." from becoming live HTML inside the email.
    private static string ResetEmailBody(string fullName, string link, int validMinutes) =>
        $"""
        <p>Hi {WebUtility.HtmlEncode(fullName)},</p>
        <p><a href="{WebUtility.HtmlEncode(link)}">Set a new password</a> - this link works once, for {validMinutes} minutes.</p>
        <p>If you didn't ask for this, ignore this email. Your password stays the same.</p>
        """;
}

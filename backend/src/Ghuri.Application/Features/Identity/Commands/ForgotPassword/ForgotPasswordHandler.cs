using Ghuri.Application.Abstractions.Messaging;
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
    PasswordLinkSender links,
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

        // One-time link, newest wins - see PasswordLinkSender.
        await links.SendResetLinkAsync(user, nowUtc, cancellationToken);

        return Result.Success();
    }
}

using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Identity.Commands.ResetPassword;

/// <summary>Checks the emailed token, sets the new password, and logs the user out everywhere.</summary>
internal sealed class ResetPasswordHandler(
    IUserRepository users,
    IOtpRepository otps,
    IRefreshTokenRepository refreshTokens,
    ITokenService tokens,
    IPasswordHasher passwords,
    TimeProvider clock) : ICommandHandler<ResetPasswordCommand>
{
    public async ValueTask<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null || user.Email is null)
            return IdentityErrors.ResetLinkInvalid;

        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var code = await otps.GetLatestAsync(User.NormalizeEmail(user.Email), OtpPurpose.ResetPassword, cancellationToken);
        if (code is null || !code.IsUsable(nowUtc))
            return IdentityErrors.ResetLinkInvalid;

        // The database only holds the HASH, so hash what the user sent and
        // compare hashes. (Comparing hashes leaks nothing useful through
        // timing: an attacker can't steer a hash toward a match.)
        if (tokens.HashOpaqueToken(command.Token) != code.CodeHash)
        {
            code.RecordWrongAttempt(); // kept by CommitChanges on the error - 5 and the link is dead
            return IdentityErrors.ResetLinkInvalid;
        }

        code.Consume(nowUtc);                                 // the link never works again
        user.SetPassword(passwords.Hash(command.NewPassword)); // also clears any lock

        // Whoever knew the OLD password - maybe the reason for the reset -
        // is logged out on every device.
        await refreshTokens.RevokeAllForUserAsync(user.Id, nowUtc, cancellationToken);

        return Result.Success();
    }
}

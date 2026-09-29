using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace Ghuri.Application.Features.Identity.Commands.ChangePassword;

internal sealed class ChangePasswordHandler(
    ICurrentUser currentUser,
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwords,
    SessionIssuer sessions,
    IOptions<AuthOptions> options,
    TimeProvider clock) : ICommandHandler<ChangePasswordCommand, AuthTokens>
{
    public async ValueTask<Result<AuthTokens>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        // The API will only let logged-in users call this, but the handler
        // checks too - it must be safe no matter who sends it.
        if (currentUser.UserId is not { } userId)
            return IdentityErrors.NotAuthenticated;

        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return IdentityErrors.NotAuthenticated;

        var nowUtc = clock.GetUtcNow().UtcDateTime;
        if (user.IsLockedOut(nowUtc))
            return IdentityErrors.AccountLocked;

        // The current password is asked for - and wrong guesses COUNT, like
        // at login - because someone who grabbed an unlocked laptop (or a
        // stolen access token) must not be able to take the account over.
        if (user.PasswordHash is null || !passwords.Verify(user.PasswordHash, command.CurrentPassword))
        {
            var settings = options.Value;
            user.RecordFailedLogin(nowUtc, settings.MaxFailedLogins, TimeSpan.FromMinutes(settings.LockoutMinutes));
            return user.IsLockedOut(nowUtc) ? IdentityErrors.AccountLocked : IdentityErrors.CurrentPasswordWrong;
        }

        user.SetPassword(passwords.Hash(command.NewPassword));

        // Log out EVERY device (including this one's old session)...
        await refreshTokens.RevokeAllForUserAsync(user.Id, nowUtc, cancellationToken);

        // ...then give THIS device a fresh session, so the user who just
        // changed their password isn't thrown out themselves.
        return sessions.Issue(user, familyId: Guid.CreateVersion7(), nowUtc);
    }
}

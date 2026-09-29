using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Ghuri.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace Ghuri.Application.Features.Identity.Commands.Login;

/// <summary>Checks the password (with lockout) and starts a new session.</summary>
internal sealed class LoginHandler(
    IUserRepository users,
    IPasswordHasher passwords,
    SessionIssuer sessions,
    IOptions<AuthOptions> options,
    TimeProvider clock) : ICommandHandler<LoginCommand, AuthTokens>
{
    public async ValueTask<Result<AuthTokens>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(command.PhoneOrEmail, cancellationToken);
        if (user is null)
            return IdentityErrors.InvalidCredentials; // same answer as a wrong password

        var nowUtc = clock.GetUtcNow().UtcDateTime;

        // Checked BEFORE the password: during a lock the password isn't
        // even looked at, so guessing during a lock is pointless.
        if (user.IsLockedOut(nowUtc))
            return IdentityErrors.AccountLocked;

        if (user.PasswordHash is null || !passwords.Verify(user.PasswordHash, command.Password))
        {
            var settings = options.Value;
            user.RecordFailedLogin(nowUtc, settings.MaxFailedLogins, TimeSpan.FromMinutes(settings.LockoutMinutes));

            // Both errors carry CommitChanges, so the count - and a lock
            // that the 5th failure just started - is saved, not rolled back.
            return user.IsLockedOut(nowUtc) ? IdentityErrors.AccountLocked : IdentityErrors.InvalidCredentials;
        }

        // Checked only AFTER the right password: a stranger typing a phone
        // number must not learn "this account exists but is disabled".
        if (user.Status != UserStatus.Active)
            return IdentityErrors.AccountDisabled;

        user.RecordSuccessfulLogin(nowUtc);
        return sessions.Issue(user, familyId: Guid.CreateVersion7(), nowUtc);
    }

    private Task<User?> FindUserAsync(string phoneOrEmail, CancellationToken cancellationToken)
    {
        // "@" decides, not PhoneNumber.TryCreate first: TryCreate keeps only
        // the digits, so an email like "01711000000@gmail.com" would
        // otherwise be mistaken for a phone number.
        if (phoneOrEmail.Contains('@'))
            return users.GetByEmailAsync(phoneOrEmail, cancellationToken);

        return PhoneNumber.TryCreate(phoneOrEmail, out var phone)
            ? users.GetByPhoneAsync(phone!, cancellationToken)
            : Task.FromResult<User?>(null);
    }
}

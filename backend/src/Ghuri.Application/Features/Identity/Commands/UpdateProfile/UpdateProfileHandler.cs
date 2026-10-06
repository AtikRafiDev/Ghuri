using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace Ghuri.Application.Features.Identity.Commands.UpdateProfile;

/// <remarks>
/// Changing the EMAIL asks for the current password, like ChangePassword:
/// password-reset links go to that email, so whoever controls it can take
/// the account over. Someone at an unlocked laptop must not be able to
/// point it at their own inbox. Wrong guesses count towards the lock, as at
/// login. Changing only the name needs no password.
/// </remarks>
internal sealed class UpdateProfileHandler(
    ICurrentUser currentUser,
    IUserRepository users,
    IPasswordHasher passwords,
    IOptions<AuthOptions> options,
    TimeProvider clock) : ICommandHandler<UpdateProfileCommand>
{
    public async ValueTask<Result> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            return IdentityErrors.NotAuthenticated;

        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return IdentityErrors.NotAuthenticated;

        var email = string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim();
        if (!User.EmailEquals(user.Email, email))
        {
            var nowUtc = clock.GetUtcNow().UtcDateTime;
            if (user.IsLockedOut(nowUtc))
                return IdentityErrors.AccountLocked;

            if (user.PasswordHash is null || string.IsNullOrEmpty(command.CurrentPassword)
                || !passwords.Verify(user.PasswordHash, command.CurrentPassword))
            {
                var settings = options.Value;
                user.RecordFailedLogin(nowUtc, settings.MaxFailedLogins, TimeSpan.FromMinutes(settings.LockoutMinutes));
                return user.IsLockedOut(nowUtc) ? IdentityErrors.AccountLocked : IdentityErrors.CurrentPasswordWrong;
            }

            // Staff log in and get reset links by email (User.Email: "required for staff").
            if (email is null && !user.Roles.All(r => r.RoleId == (byte)SystemRole.Customer))
                return IdentityErrors.StaffEmailRequired;

            if (email is not null && await users.EmailExistsAsync(email, cancellationToken))
                return IdentityErrors.EmailTaken;
        }

        user.UpdateProfile(command.FullName, email);
        return Result.Success();
    }
}

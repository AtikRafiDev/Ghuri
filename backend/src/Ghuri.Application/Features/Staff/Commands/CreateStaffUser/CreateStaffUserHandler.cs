using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Identity;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Repositories;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Application.Features.Staff.Commands.CreateStaffUser;

internal sealed class CreateStaffUserHandler(
    IUserRepository users,
    PasswordLinkSender links,
    TimeProvider clock) : ICommandHandler<CreateStaffUserCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CreateStaffUserCommand command, CancellationToken cancellationToken)
    {
        // A number or email that already has an account - often the person's
        // own customer account - is refused: one person, one login per number.
        var phone = PhoneNumber.Create(command.Phone); // safe: the validator already checked it
        if (await users.PhoneExistsAsync(phone, cancellationToken))
            return IdentityErrors.PhoneTaken;

        var email = command.Email.Trim();
        if (await users.EmailExistsAsync(email, cancellationToken))
            return IdentityErrors.EmailTaken;

        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var user = User.CreateStaff(command.FullName, phone, email, command.Role, nowUtc);
        users.Add(user);

        // If the email can't be sent, this throws and the transaction rolls
        // back - no half-made account without a way in.
        await links.SendStaffInviteAsync(user, command.Role.ToString(), nowUtc, cancellationToken);

        return user.Id;
    }
}

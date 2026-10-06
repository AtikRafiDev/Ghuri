using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Identity;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Staff.Commands.SendStaffPasswordLink;

internal sealed class SendStaffPasswordLinkHandler(
    IUserRepository users,
    PasswordLinkSender links,
    TimeProvider clock) : ICommandHandler<SendStaffPasswordLinkCommand>
{
    public async ValueTask<Result> Handle(SendStaffPasswordLinkCommand command, CancellationToken cancellationToken)
    {
        var found = await users.GetEditableAsync(command.Id, cancellationToken);
        if (found.IsFailure)
            return found.Error;

        var user = found.Value;
        if (user.Status != UserStatus.Active)
            return StaffErrors.AccountDisabled; // a link they couldn't use - login would still refuse them
        if (user.Email is null)
            return IdentityErrors.StaffEmailRequired; // older accounts made before staff needed one

        await links.SendStaffInviteAsync(user, user.StaffRole().ToString(), clock.GetUtcNow().UtcDateTime, cancellationToken);
        return Result.Success();
    }
}

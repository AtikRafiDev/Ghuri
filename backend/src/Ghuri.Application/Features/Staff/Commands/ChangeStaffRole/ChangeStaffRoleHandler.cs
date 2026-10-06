using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Staff.Commands.ChangeStaffRole;

internal sealed class ChangeStaffRoleHandler(IUserRepository users, TimeProvider clock)
    : ICommandHandler<ChangeStaffRoleCommand>
{
    public async ValueTask<Result> Handle(ChangeStaffRoleCommand command, CancellationToken cancellationToken)
    {
        var found = await users.GetEditableAsync(command.Id, cancellationToken);
        if (found.IsFailure)
            return found.Error;

        found.Value.ChangeStaffRole(command.Role, clock.GetUtcNow().UtcDateTime);
        return Result.Success();
    }
}

using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Staff.Commands.EnableStaffUser;

internal sealed class EnableStaffUserHandler(IUserRepository users) : ICommandHandler<EnableStaffUserCommand>
{
    public async ValueTask<Result> Handle(EnableStaffUserCommand command, CancellationToken cancellationToken)
    {
        var found = await users.GetEditableAsync(command.Id, cancellationToken);
        if (found.IsFailure)
            return found.Error;

        found.Value.Enable();
        return Result.Success();
    }
}

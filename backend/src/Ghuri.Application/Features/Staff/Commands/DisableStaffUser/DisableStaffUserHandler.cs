using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Staff.Commands.DisableStaffUser;

internal sealed class DisableStaffUserHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    TimeProvider clock) : ICommandHandler<DisableStaffUserCommand>
{
    public async ValueTask<Result> Handle(DisableStaffUserCommand command, CancellationToken cancellationToken)
    {
        var found = await users.GetEditableAsync(command.Id, cancellationToken);
        if (found.IsFailure)
            return found.Error;

        var user = found.Value;
        user.Disable();

        // Logged out on every device now, not at the next refresh.
        await refreshTokens.RevokeAllForUserAsync(user.Id, clock.GetUtcNow().UtcDateTime, cancellationToken);
        return Result.Success();
    }
}

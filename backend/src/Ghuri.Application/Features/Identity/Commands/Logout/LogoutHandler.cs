using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Identity.Commands.Logout;

/// <summary>Revokes this login's refresh tokens. Always succeeds.</summary>
/// <remarks>
/// Works without a valid access token on purpose: someone whose access
/// token already expired must still be able to log out properly.
/// The access token itself can't be "revoked" - it simply stops working
/// within 15 minutes, and the frontend throws it away immediately.
/// </remarks>
internal sealed class LogoutHandler(
    IRefreshTokenRepository refreshTokens,
    ITokenService tokens,
    TimeProvider clock) : ICommandHandler<LogoutCommand>
{
    public async ValueTask<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.RefreshToken))
            return Result.Success();

        var current = await refreshTokens.GetByHashAsync(tokens.HashOpaqueToken(command.RefreshToken), cancellationToken);
        if (current is not null)
        {
            // The whole family, not just this token: it ends THIS login on
            // THIS device, while the user's other devices stay logged in.
            await refreshTokens.RevokeFamilyAsync(current.FamilyId, clock.GetUtcNow().UtcDateTime, cancellationToken);
        }

        return Result.Success();
    }
}

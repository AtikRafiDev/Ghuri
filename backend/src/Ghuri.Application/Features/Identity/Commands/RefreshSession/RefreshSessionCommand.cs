using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Identity.Commands.RefreshSession;

/// <summary>
/// Trade the refresh token (from the HttpOnly cookie) for a new access
/// token AND a new refresh token - the old one is spent (rotation).
/// </summary>
/// <remarks>
/// The blueprint calls this "RefreshToken"; renamed because a folder and
/// namespace named RefreshToken would clash with the RefreshToken entity
/// inside every file of this use case.
/// </remarks>
public sealed record RefreshSessionCommand(string RefreshToken) : ICommand<AuthTokens>;

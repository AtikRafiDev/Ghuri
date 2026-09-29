using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Identity.Commands.Logout;

/// <summary>
/// Ends this device's login. RefreshToken is the cookie's value, or null
/// if there's no cookie - logging out twice is fine, never an error.
/// </summary>
public sealed record LogoutCommand(string? RefreshToken) : ICommand;

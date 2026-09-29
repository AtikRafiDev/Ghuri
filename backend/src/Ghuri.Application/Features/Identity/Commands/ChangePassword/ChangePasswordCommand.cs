using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Identity.Commands.ChangePassword;

/// <summary>
/// A logged-in user changes their password. Returns a NEW session for the
/// device they're on; every other device is logged out.
/// </summary>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand<AuthTokens>;

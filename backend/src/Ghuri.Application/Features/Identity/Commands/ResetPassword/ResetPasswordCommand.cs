using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Identity.Commands.ResetPassword;

/// <summary>Sets a new password using the Email and Token from the reset link.</summary>
public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword) : ICommand;

using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Identity.Commands.ForgotPassword;

/// <summary>"Email me a link to set a new password." Always succeeds - see the handler for why.</summary>
public sealed record ForgotPasswordCommand(string Email) : ICommand;

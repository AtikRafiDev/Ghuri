using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Identity.Commands.Login;

/// <summary>Log in with a phone number OR an email, plus the password (14-day plan, Day 2: "Email/phone + password login").</summary>
public sealed record LoginCommand(string PhoneOrEmail, string Password) : ICommand<AuthTokens>;

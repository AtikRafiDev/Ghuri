using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Identity.Commands.RegisterCustomer;

/// <summary>
/// Creates a customer account and logs them straight in - returns a
/// session, so the customer doesn't have to type their details twice.
/// </summary>
public sealed record RegisterCustomerCommand(string FullName, string Phone, string? Email, string Password)
    : ICommand<AuthTokens>;

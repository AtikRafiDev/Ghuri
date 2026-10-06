using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Domain.Enums;

namespace Ghuri.Application.Features.Staff.Commands.CreateStaffUser;

/// <summary>
/// The Super Admin adds a staff member (Admin → Staff). Customers sign up
/// themselves; staff accounts are only ever made here. No password is
/// sent: the new staff member gets an email with a link to set their own.
/// Returns the new account's id.
/// </summary>
/// <remarks>
/// They can log in with the Phone or the Email. Email is required: the
/// welcome link and later password resets go there. Role: Manager, Sales or Accounts.
/// </remarks>
public sealed record CreateStaffUserCommand(string FullName, string Phone, string Email, SystemRole Role) : ICommand<Guid>;

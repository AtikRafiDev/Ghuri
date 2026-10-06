using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Staff.Commands.SendStaffPasswordLink;

/// <summary>
/// Emails a staff member a new "set your password" link - the welcome email
/// got lost, its link ran out, or they forgot their password. Older links
/// stop working. The admin still never sees or sets the password.
/// </summary>
public sealed record SendStaffPasswordLinkCommand(Guid Id) : ICommand;

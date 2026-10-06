using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Domain.Enums;

namespace Ghuri.Application.Features.Staff.Commands.ChangeStaffRole;

/// <summary>
/// Makes a staff member Manager, Sales or Accounts, replacing their current
/// role. Id comes from the URL (PUT /admin/staff/{id}/role). Takes effect at
/// their next login or token refresh - within 15 minutes.
/// </summary>
public sealed record ChangeStaffRoleCommand(Guid Id, SystemRole Role) : ICommand;

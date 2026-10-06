using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Staff.Commands.EnableStaffUser;

/// <summary>Lets a disabled staff member log in again, with the password they had.</summary>
public sealed record EnableStaffUserCommand(Guid Id) : ICommand;

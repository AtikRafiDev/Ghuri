using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Staff.Commands.DisableStaffUser;

/// <summary>
/// Stops a staff member logging in - e.g. they left the agency. Their
/// sessions end at once; an access token already handed out still works
/// for at most 15 minutes. Nothing is deleted: their name stays on the
/// bookings, payments and refunds they handled. Can be undone (Enable).
/// </summary>
public sealed record DisableStaffUserCommand(Guid Id) : ICommand;

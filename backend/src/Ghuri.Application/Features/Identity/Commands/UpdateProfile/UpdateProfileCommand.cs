using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Identity.Commands.UpdateProfile;

/// <summary>
/// The logged-in user changes their own name and email (17-day plan,
/// Day 11: UpdateProfile). The phone number stays - it's their login, and
/// changing it needs an SMS code (Phase 2).
/// </summary>
/// <param name="FullName">As shown on bookings and emails.</param>
/// <param name="Email">Empty = no email (customers only - staff must keep one).</param>
/// <param name="CurrentPassword">Needed only when the email CHANGES (see the handler).</param>
public sealed record UpdateProfileCommand(string FullName, string? Email, string? CurrentPassword) : ICommand;

using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Commands.CloseDeparture;

/// <summary>Stops new bookings on a departure (full, or no longer sold). Existing bookings keep their seats.</summary>
public sealed record CloseDepartureCommand(Guid Id) : ICommand;

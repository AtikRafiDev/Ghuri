using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Commands.DeleteDestination;

/// <summary>
/// Soft delete: the row stays (old bookings and the audit log still point at
/// it) but disappears from every list, and its name/slug become free again.
/// </summary>
public sealed record DeleteDestinationCommand(Guid Id) : ICommand;

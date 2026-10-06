using Ghuri.Domain.Common;

namespace Ghuri.Domain.Entities.Booking;

/// <summary>
/// A booking was paid and confirmed (17-day plan, Day 11: "BookingConfirmed
/// event (outbox) → email with PDFs"). Saved to ops.OutboxMessages in the
/// same transaction as the confirmation, then handled by the outbox job.
/// </summary>
/// <remarks>
/// Only the id: whoever handles it reads the booking fresh. A copy of the
/// details here could already be out of date by the time the email goes out.
/// </remarks>
public sealed record BookingConfirmed(Guid BookingId) : IDomainEvent;

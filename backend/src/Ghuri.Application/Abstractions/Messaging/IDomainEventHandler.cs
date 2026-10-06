using Ghuri.Domain.Common;

namespace Ghuri.Application.Abstractions.Messaging;

/// <summary>
/// Reacts to a domain event AFTER it was saved (the outbox job calls it),
/// e.g. "BookingConfirmed → email the voucher". Never inside the
/// transaction that raised the event: a slow mail server must never hold up
/// - or roll back - a payment confirmation.
/// </summary>
/// <remarks>
/// <para>
/// At least once: if sending works but marking the event done fails (the
/// database blipped), the event is handled again on the next run. Handlers
/// should cope with that - a second identical email is acceptable, a second
/// charge or refund is not.
/// </para>
/// <para>
/// Throw to say "failed, try again later": the outbox job records the error
/// and retries a few times. Registered automatically (AddApplication).
/// </para>
/// </remarks>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}

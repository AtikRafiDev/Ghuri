using System.Text.Json;
using Ghuri.Domain.Common;
using Ghuri.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Ghuri.Infrastructure.Persistence.Interceptors;

/// <summary>
/// The write half of the transactional outbox (blueprint section 4): right
/// before EVERY SaveChanges, the domain events raised by the aggregates
/// being saved (e.g. Booking.Confirm → BookingConfirmed) become
/// ops.OutboxMessages rows - in the SAME SaveChanges, so "booking confirmed"
/// and "send the voucher" are saved together or not at all.
/// </summary>
/// <remarks>
/// Why not send the email right here? If the transaction then failed, the
/// customer would get a voucher for a booking that doesn't exist; and a
/// slow mail server would hold the booking's locks. Instead
/// OutboxDispatcherJob picks the rows up a moment later.
/// A command that FAILS never reaches SaveChanges (EfUnitOfWork rolls back),
/// so its events are never written.
/// </remarks>
internal sealed class OutboxInterceptor(TimeProvider clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            WriteOutbox(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            WriteOutbox(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void WriteOutbox(DbContext context)
    {
        var aggregates = context.ChangeTracker.Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();
        if (aggregates.Count == 0)
            return;

        var nowUtc = clock.GetUtcNow().UtcDateTime;
        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                // The full type name ("Ghuri.Domain.Entities.Booking.BookingConfirmed")
                // lets the dispatcher turn the JSON back into the right event.
                var type = domainEvent.GetType();
                context.Set<OutboxMessage>().Add(OutboxMessage.Create(
                    type.FullName!, JsonSerializer.Serialize(domainEvent, type), nowUtc));
            }

            // Written once: a second SaveChanges must not queue them again.
            aggregate.ClearDomainEvents();
        }
    }
}

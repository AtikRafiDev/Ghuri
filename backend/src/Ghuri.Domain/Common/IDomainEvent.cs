namespace Ghuri.Domain.Common;

/// <summary>
/// Marker for something that happened in the Domain that other parts of
/// the system might care about (e.g. BookingConfirmed, PaymentSucceeded).
/// Raised by an AggregateRoot, saved to the ops.OutboxMessages table in
/// the SAME transaction as the data change, and handled afterwards - an
/// email gets sent, a cache gets cleared. See the blueprint's
/// "Transactional outbox" pattern (section 4).
/// </summary>
/// <remarks>
/// Deliberately empty for now. Concrete events (BookingConfirmed etc.)
/// get their own properties when we build the feature they belong to -
/// there is nothing shared to put here yet.
/// </remarks>
public interface IDomainEvent;

using Ghuri.Domain.Entities.Booking;

namespace Ghuri.Domain.Repositories;

/// <summary>The write side for bookings (one of the plan's five repositories: Booking, Departure, Payment, TourPackage, CustomTrip).</summary>
public interface IBookingRepository
{
    /// <summary>The next booking number - TB100001, TB100002... - from a database SEQUENCE, so two customers never get the same one.</summary>
    Task<string> NextBookingNoAsync(CancellationToken cancellationToken);

    /// <summary>The booking itself, tracked for changes - without its travellers or add-ons (status changes don't need them).</summary>
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>A booking by its number (TB100001), tracked - without travellers or add-ons.</summary>
    Task<Booking?> GetByBookingNoAsync(string bookingNo, CancellationToken cancellationToken);

    /// <summary>
    /// Like GetByIdAsync, but LOCKS the row until the transaction ends: anyone
    /// else changing this booking waits for us. Used by the payment
    /// confirmation, so the expiry job can't expire a booking halfway through
    /// confirming it.
    /// </summary>
    Task<Booking?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>GetByIdForUpdateAsync by the booking's number - e.g. a customer cancelling while their payment is being confirmed.</summary>
    Task<Booking?> GetByBookingNoForUpdateAsync(string bookingNo, CancellationToken cancellationToken);

    /// <summary>The customer's bookings still waiting for payment inside their 20-minute hold - each one holds seats.</summary>
    Task<int> CountUnpaidHoldsAsync(Guid customerId, DateTime nowUtc, CancellationToken cancellationToken);

    void Add(Booking booking);
}

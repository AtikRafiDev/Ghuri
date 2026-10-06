using Ghuri.Domain.Entities.Booking;

namespace Ghuri.Domain.Repositories;

/// <summary>The write side for custom trips (the last of the plan's five repositories).</summary>
public interface ICustomTripRepository
{
    /// <summary>The next trip number - CT1001, CT1002... - from a database SEQUENCE.</summary>
    Task<string> NextTripNoAsync(CancellationToken cancellationToken);

    /// <summary>
    /// A trip by its number, tracked, WITH its legs and quote lines - and
    /// LOCKED until the transaction ends: a re-quote, a cancel and the expiry
    /// job touching the same trip run one after the other.
    /// </summary>
    Task<CustomTrip?> GetByTripNoForUpdateAsync(string tripNo, CancellationToken cancellationToken);

    /// <summary>The same by id (the expiry job knows ids).</summary>
    Task<CustomTrip?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    void Add(CustomTrip trip);
}

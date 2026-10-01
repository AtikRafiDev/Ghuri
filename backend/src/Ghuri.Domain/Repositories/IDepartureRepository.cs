using Ghuri.Domain.Entities.Catalog;

namespace Ghuri.Domain.Repositories;

/// <summary>
/// Departures (blueprint section 7.3). The two seat methods are the
/// reason this repository exists: they change the seat count with ONE
/// atomic SQL statement, so two customers can never both get the last seat.
/// </summary>
public interface IDepartureRepository
{
    Task<Departure?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>All of a package's departures, any status - tracked, so changes to them are saved.</summary>
    Task<IReadOnlyList<Departure>> ListForPackageAsync(Guid packageId, CancellationToken cancellationToken);

    /// <summary>One departure per package per day (a unique index too). exceptId = the one being edited.</summary>
    Task<bool> ExistsOnDateAsync(Guid packageId, DateOnly startDate, Guid? exceptId, CancellationToken cancellationToken);

    /// <summary>
    /// The lowest adult price among OPEN departures starting on or after
    /// <paramref name="today"/>, ignoring <paramref name="exceptId"/> (the one
    /// being changed right now, not saved yet). Null = none.
    /// </summary>
    Task<decimal?> LowestOpenAdultPriceAsync(Guid packageId, DateOnly today, Guid? exceptId, CancellationToken cancellationToken);

    /// <summary>Is there an OPEN departure starting on or after <paramref name="today"/>?</summary>
    Task<bool> HasOpenUpcomingAsync(Guid packageId, DateOnly today, CancellationToken cancellationToken);

    /// <summary>
    /// Takes <paramref name="seats"/> seats if - and only if - the departure is
    /// open and still has that many free. True = taken; false = not enough
    /// seats (or closed). Runs immediately in the database, inside the
    /// current transaction.
    /// </summary>
    Task<bool> TryReserveSeatsAsync(Guid departureId, short seats, CancellationToken cancellationToken);

    /// <summary>Gives seats back (expired hold, cancelled booking). Never lets the count go below 0. True = released.</summary>
    Task<bool> ReleaseSeatsAsync(Guid departureId, short seats, CancellationToken cancellationToken);

    void Add(Departure departure);
}

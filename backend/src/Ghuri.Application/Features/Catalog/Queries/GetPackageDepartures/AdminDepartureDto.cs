using Ghuri.Domain.Enums;

namespace Ghuri.Application.Features.Catalog.Queries.GetPackageDepartures;

/// <summary>One row of the Departures tab - also everything its edit dialog needs.</summary>
/// <remarks>IsPast: started before today (Bangladesh time) - shown greyed, can't be booked any more.</remarks>
public sealed record AdminDepartureDto(
    Guid Id,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal AdultPrice,
    decimal ChildPrice,
    decimal InfantPrice,
    decimal? SingleSupplement,
    int TotalSeats,
    int ReservedSeats,
    int SeatsLeft,
    int BookingCutoffDays,
    DepartureStatus Status,
    bool IsPast);

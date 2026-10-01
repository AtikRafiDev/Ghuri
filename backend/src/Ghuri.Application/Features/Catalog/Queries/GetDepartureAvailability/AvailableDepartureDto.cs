namespace Ghuri.Application.Features.Catalog.Queries.GetDepartureAvailability;

/// <summary>One date on the package page's picker. SeatsLeft 0 = shown as "Sold out", can't be picked.</summary>
/// <remarks>LastBookingDate: the last day this date can be booked (StartDate - booking cutoff).</remarks>
public sealed record AvailableDepartureDto(
    Guid Id,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal AdultPrice,
    decimal ChildPrice,
    decimal InfantPrice,
    decimal? SingleSupplement,
    int SeatsLeft,
    DateOnly LastBookingDate);

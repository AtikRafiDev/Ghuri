using Ghuri.Domain.Enums;

namespace Ghuri.Application.Features.Booking.Queries.GetBookingQuote;

/// <summary>
/// The price of one trip, line by line - what the package page and the
/// checkout show. EndDate: the last day of a fixed trip, or the check-out
/// day of a flexible stay (StartDate + Nights).
/// </summary>
public sealed record BookingQuoteDto(
    Guid PackageId,
    string PackageTitle,
    PricingMode PricingMode,
    Guid? DepartureId,
    DateOnly StartDate,
    DateOnly EndDate,
    int Nights,
    int Adults,
    int Children,
    int Infants,
    int SingleRooms,
    IReadOnlyList<PriceLineDto> Lines,
    decimal Total,
    string Currency);

public sealed record PriceLineDto(string Label, decimal UnitPrice, int Quantity, decimal Amount);

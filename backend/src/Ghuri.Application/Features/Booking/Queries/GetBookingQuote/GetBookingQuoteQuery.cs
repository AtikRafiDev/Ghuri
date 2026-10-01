using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Booking.Queries.GetBookingQuote;

/// <summary>
/// What a trip would cost - changes nothing, reserves nothing
/// (17-day plan, Day 6: GetBookingQuote).
/// </summary>
/// <remarks>
/// Fixed-departure package: send DepartureId. Flexible stay: send StartDate
/// and Nights. The other pair is ignored.
/// </remarks>
public sealed record GetBookingQuoteQuery(
    string Slug,
    Guid? DepartureId,
    DateOnly? StartDate,
    int? Nights,
    int Adults,
    int Children = 0,
    int Infants = 0,
    int SingleRooms = 0) : IQuery<BookingQuoteDto>;

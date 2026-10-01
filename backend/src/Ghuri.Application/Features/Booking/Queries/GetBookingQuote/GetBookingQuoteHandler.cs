using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Services;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Booking.Queries.GetBookingQuote;

/// <summary>
/// Loads the package (and departure), asks the DOMAIN whether the trip can
/// be booked and what it costs, and returns the answer. No rule lives here:
/// Day 8's CreateBooking asks the same domain methods, so a quote and the
/// real booking can never disagree.
/// </summary>
internal sealed class GetBookingQuoteHandler(IReadDbContext db, TimeProvider clock)
    : IQueryHandler<GetBookingQuoteQuery, BookingQuoteDto>
{
    public async ValueTask<Result<BookingQuoteDto>> Handle(GetBookingQuoteQuery query, CancellationToken cancellationToken)
    {
        var slug = Slug.Create(query.Slug);
        // Only what's for sale: a draft or archived package is "not found" to the public.
        var package = await db.TourPackages
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == PackageStatus.Published, cancellationToken);
        if (package is null)
            return BookingErrors.PackageNotFound;

        var travellers = Travellers.Create(query.Adults, query.Children, query.Infants); // already validated
        var today = clock.Today();

        return package.PricingMode == PricingMode.FixedDepartures
            ? await QuoteDepartureAsync(package, query, travellers, today, cancellationToken)
            : QuoteFlexibleStay(package, query, travellers, today);
    }

    private async Task<Result<BookingQuoteDto>> QuoteDepartureAsync(
        TourPackage package, GetBookingQuoteQuery query, Travellers travellers, DateOnly today, CancellationToken cancellationToken)
    {
        if (query.DepartureId is not { } departureId)
            return BookingErrors.DepartureRequired;

        var departure = await db.Departures
            .FirstOrDefaultAsync(d => d.Id == departureId && d.PackageId == package.Id, cancellationToken);
        if (departure is null)
            return BookingErrors.DepartureNotFound;

        switch (departure.CheckBookable(today, travellers.Seats))
        {
            case DepartureBookability.NotOpen:
                return BookingErrors.DepartureNotBookable;
            case DepartureBookability.BookingClosed:
                return BookingErrors.BookingClosed(departure.LastBookingDate);
            case DepartureBookability.NotEnoughSeats:
                return BookingErrors.NotEnoughSeats(departure.SeatsLeft);
        }

        if (query.SingleRooms > 0 && departure.SingleSupplement is null)
            return BookingErrors.SingleRoomNotOffered;

        var price = PriceCalculator.ForDeparture(package, departure, travellers, query.SingleRooms);
        return ToDto(package, departure.Id, departure.StartDate, departure.EndDate, package.DurationNights, query, travellers, price);
    }

    private static Result<BookingQuoteDto> QuoteFlexibleStay(
        TourPackage package, GetBookingQuoteQuery query, Travellers travellers, DateOnly today)
    {
        if (query.StartDate is not { } startDate || query.Nights is not { } nights)
            return BookingErrors.FlexibleDatesRequired;

        // A flexible stay is a hotel room booked for the guests - no "single supplement".
        if (query.SingleRooms > 0)
            return BookingErrors.SingleRoomNotOffered;

        switch (package.CheckFlexibleStay(today, startDate, nights))
        {
            case FlexibleStayBookability.NightsOutOfRange:
                return BookingErrors.NightsOutOfRange(package.MinNights!.Value, package.MaxNights!.Value);
            case FlexibleStayBookability.TooSoon:
                return BookingErrors.StartDateTooSoon(package.EarliestFlexibleStart(today));
        }

        var price = PriceCalculator.ForFlexibleStay(package, nights, travellers);
        // EndDate = the check-out day: 2 nights from the 20th = out on the 22nd.
        return ToDto(package, null, startDate, startDate.AddDays(nights), nights, query, travellers, price);
    }

    private static BookingQuoteDto ToDto(
        TourPackage package, Guid? departureId, DateOnly startDate, DateOnly endDate, int nights,
        GetBookingQuoteQuery query, Travellers travellers, PriceBreakdown price) =>
        new(
            package.Id,
            package.Title,
            package.PricingMode,
            departureId,
            startDate,
            endDate,
            nights,
            travellers.Adults,
            travellers.Children,
            travellers.Infants,
            query.SingleRooms,
            price.Lines.Select(l => new PriceLineDto(l.Label, l.UnitPrice, l.Quantity, l.Amount)).ToList(),
            price.Total,
            price.Currency);
}

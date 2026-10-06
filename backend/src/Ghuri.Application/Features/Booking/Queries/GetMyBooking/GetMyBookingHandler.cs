using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Booking.Queries.GetMyBooking;

internal sealed class GetMyBookingHandler(IReadDbContext db, ICurrentUser currentUser, CancellationTerms terms)
    : IQueryHandler<GetMyBookingQuery, MyBookingDto>
{
    public async ValueTask<Result<MyBookingDto>> Handle(GetMyBookingQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } customerId)
            return IdentityErrors.NotAuthenticated;

        // CustomerId is part of the WHERE: another customer's booking simply
        // isn't found. A left join, because a custom trip (Day 15) has no package.
        var row = await (
                from b in db.Bookings
                where b.BookingNo == query.BookingNo && b.CustomerId == customerId
                join p in db.TourPackages on b.PackageId equals p.Id into packages
                from p in packages.DefaultIfEmpty()
                select new { Booking = b, PackageTitle = p == null ? null : p.Title, PackageSlug = p == null ? null : p.Slug })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null)
            return BookingErrors.BookingNotFound;

        var booking = row.Booking;
        var travellers = await db.BookingTravellers
            .Where(t => t.BookingId == booking.Id)
            .OrderByDescending(t => t.IsLead) // the lead traveller first
            .Select(t => new MyBookingTravellerDto(t.FullName, t.TravellerType, t.IsLead))
            .ToListAsync(cancellationToken);

        var cancellation = await terms.QuoteAsync(booking.Status, booking.PackageId, booking.StartDate, booking.PaidAmount, cancellationToken);
        var refund = await db.Refunds
            .Where(r => r.BookingId == booking.Id)
            .OrderByDescending(r => r.RefundNo.Length).ThenByDescending(r => r.RefundNo) // RF999 < RF1000: by length first
            .Select(r => new MyBookingRefundDto(r.RefundNo, r.Amount, r.Status))
            .FirstOrDefaultAsync(cancellationToken);

        return new MyBookingDto(
            booking.Id, booking.BookingNo, booking.BookingType, booking.Status,
            row.PackageTitle, row.PackageSlug?.Value,
            booking.StartDate, booking.EndDate, booking.Nights,
            booking.Adults, booking.Children, booking.Infants,
            booking.AdultPriceSnapshot, booking.ChildPriceSnapshot, booking.InfantPriceSnapshot,
            booking.TotalAmount, booking.PaidAmount, booking.Currency, booking.HoldExpiresAtUtc,
            booking.ContactName, booking.ContactPhone.Value, booking.ContactEmail, booking.SpecialRequest,
            travellers, cancellation, refund, booking.CancelledAtUtc);
    }
}

using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Booking.Documents;

/// <summary>
/// Reads everything the invoice and the e-voucher need about one booking.
/// Shared by the download (GetMyBookingDocument) and the confirmation email
/// (SendBookingConfirmation), so both always show exactly the same.
/// </summary>
/// <remarks>
/// The price lines come from the booking's SNAPSHOT prices (taken when it was
/// booked) - later price changes on the package never change an invoice.
/// </remarks>
internal sealed class BookingDocumentLoader(IReadDbContext db, TimeProvider clock)
{
    public async Task<BookingDocumentData?> LoadAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var row = await (
                from booking in db.Bookings
                where booking.Id == bookingId
                join p in db.TourPackages on booking.PackageId equals p.Id into packages
                from p in packages.DefaultIfEmpty()
                select new { Booking = booking, PackageTitle = p == null ? null : p.Title })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null)
            return null;

        var b = row.Booking;
        var travellers = await db.BookingTravellers
            .Where(t => t.BookingId == b.Id)
            .OrderByDescending(t => t.IsLead)
            .Select(t => new DocumentTraveller(t.FullName, t.TravellerType, t.IsLead))
            .ToListAsync(cancellationToken);
        var addOns = await db.BookingAddOns
            .Where(a => a.BookingId == b.Id)
            .Select(a => new DocumentPriceLine(a.NameSnapshot, a.Quantity, a.UnitPrice, a.LineTotal))
            .ToListAsync(cancellationToken);
        var payments = await db.Payments
            .Where(p => p.BookingId == b.Id && p.Status == PaymentStatus.Succeeded && p.PaidAtUtc != null)
            .OrderBy(p => p.PaidAtUtc)
            .Select(p => new DocumentPayment(p.PaymentNo, p.Method, p.PaidAtUtc!.Value, p.Amount))
            .ToListAsync(cancellationToken);

        var lines = new List<DocumentPriceLine>();
        AddLine(lines, "Adult", b.Adults, b.AdultPriceSnapshot);
        AddLine(lines, "Child", b.Children, b.ChildPriceSnapshot);
        AddLine(lines, "Infant", b.Infants, b.InfantPriceSnapshot);
        lines.AddRange(addOns);
        if (b.DiscountAmount > 0)
            lines.Add(new DocumentPriceLine("Discount", 1, -b.DiscountAmount, -b.DiscountAmount));

        return new BookingDocumentData(
            b.BookingNo, b.BookingType, b.Status,
            row.PackageTitle ?? "Custom trip",
            b.StartDate, b.EndDate, b.Nights,
            b.ContactName, b.ContactPhone.Value, b.ContactEmail, b.SpecialRequest,
            travellers, lines,
            b.TotalAmount, b.PaidAmount, b.Currency,
            payments,
            clock.Today());
    }

    // A free infant still gets a line ("Infant × 1 - 0"): the customer sees everyone was counted.
    private static void AddLine(List<DocumentPriceLine> lines, string description, int quantity, decimal unitPrice)
    {
        if (quantity > 0)
            lines.Add(new DocumentPriceLine(description, quantity, unitPrice, quantity * unitPrice));
    }
}

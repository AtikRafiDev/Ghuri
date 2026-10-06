using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Booking.Documents;
using Ghuri.Application.Features.Identity;
using Ghuri.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Booking.Queries.GetMyBookingDocument;

public enum BookingDocumentKind
{
    Invoice = 1,
    Voucher = 2
}

/// <summary>
/// Download the invoice or the e-voucher of one of your own bookings as a
/// PDF (17-day plan, Day 11: "booking details with invoice/voucher download").
/// Someone else's booking is "not found", like GetMyBooking.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Invoice: once anything was paid - also after cancelling, as the receipt for the money.</item>
/// <item>Voucher: only while the booking is Confirmed (or Completed) - a voucher for a
/// cancelled or unpaid booking would be shown at a hotel that expects nobody.</item>
/// </list>
/// Made fresh on every download, from the booking as it is now.
/// </remarks>
public sealed record GetMyBookingDocumentQuery(string BookingNo, BookingDocumentKind Kind) : IQuery<BookingDocumentFile>;

/// <summary>A ready-made PDF: "Invoice-TB100001.pdf" or "Voucher-TB100001.pdf".</summary>
public sealed record BookingDocumentFile(string FileName, byte[] Content)
{
    public const string ContentType = "application/pdf";
}

internal sealed class GetMyBookingDocumentHandler(
    IReadDbContext db,
    ICurrentUser currentUser,
    BookingDocumentLoader loader,
    IBookingDocumentRenderer renderer) : IQueryHandler<GetMyBookingDocumentQuery, BookingDocumentFile>
{
    public async ValueTask<Result<BookingDocumentFile>> Handle(GetMyBookingDocumentQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } customerId)
            return IdentityErrors.NotAuthenticated;

        var bookingId = await db.Bookings
            .Where(b => b.BookingNo == query.BookingNo && b.CustomerId == customerId)
            .Select(b => (Guid?)b.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var data = bookingId is { } id ? await loader.LoadAsync(id, cancellationToken) : null;
        if (data is null)
            return BookingErrors.BookingNotFound;

        if (query.Kind == BookingDocumentKind.Voucher)
        {
            if (data.Status is not (BookingStatus.Confirmed or BookingStatus.Completed))
                return BookingErrors.DocumentNotAvailable("The e-voucher is available once the booking is confirmed.");
            return new BookingDocumentFile($"Voucher-{data.BookingNo}.pdf", renderer.RenderVoucher(data));
        }

        if (data.PaidAmount <= 0)
            return BookingErrors.DocumentNotAvailable("The invoice is available once the booking is paid.");
        return new BookingDocumentFile($"Invoice-{data.BookingNo}.pdf", renderer.RenderInvoice(data));
    }
}

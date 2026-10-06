using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Booking.Documents;
using Ghuri.Application.Features.Booking.Queries.GetMyBookingDocument;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Booking.Queries.GetBookingDocumentForAdmin;

/// <summary>
/// Staff download any booking's invoice or e-voucher (Day 12) - e.g. to
/// send it again by WhatsApp. The same rules and PDFs as the customer's own
/// download (BookingDocumentFile.Create); only "whose booking" isn't checked.
/// </summary>
public sealed record GetBookingDocumentForAdminQuery(string BookingNo, BookingDocumentKind Kind) : IQuery<BookingDocumentFile>;

internal sealed class GetBookingDocumentForAdminHandler(
    IReadDbContext db,
    BookingDocumentLoader loader,
    IBookingDocumentRenderer renderer) : IQueryHandler<GetBookingDocumentForAdminQuery, BookingDocumentFile>
{
    public async ValueTask<Result<BookingDocumentFile>> Handle(GetBookingDocumentForAdminQuery query, CancellationToken cancellationToken)
    {
        var bookingId = await db.Bookings
            .Where(b => b.BookingNo == query.BookingNo)
            .Select(b => (Guid?)b.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var data = bookingId is { } id ? await loader.LoadAsync(id, cancellationToken) : null;
        if (data is null)
            return BookingErrors.BookingNotFound;

        return BookingDocumentFile.Create(data, query.Kind, renderer);
    }
}

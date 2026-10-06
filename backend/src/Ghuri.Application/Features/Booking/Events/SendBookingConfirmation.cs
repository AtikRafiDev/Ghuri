using System.Globalization;
using System.Net;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Features.Booking.Documents;
using Ghuri.Application.Features.Booking.Queries.GetMyBookingDocument;
using Ghuri.Domain.Entities.Booking;
using Microsoft.Extensions.Logging;

namespace Ghuri.Application.Features.Booking.Events;

/// <summary>
/// BookingConfirmed → "You're booked!" email to the booking's contact, with
/// the invoice and the e-voucher attached (17-day plan, Day 11:
/// "BookingConfirmed event (outbox) → email with PDFs"). Called by the
/// outbox job a few seconds after the payment confirmed the booking - never
/// inside that transaction.
/// </summary>
/// <remarks>
/// The PDFs come from BookingDocumentLoader + the renderer, exactly like the
/// download buttons in "My bookings". No email address (only possible for
/// staff-made bookings later) → nothing to send; logged, not an error, so
/// the outbox doesn't retry it for nothing.
/// </remarks>
internal sealed class SendBookingConfirmation(
    BookingDocumentLoader loader,
    IBookingDocumentRenderer renderer,
    IEmailSender emails,
    ILogger<SendBookingConfirmation> logger) : IDomainEventHandler<BookingConfirmed>
{
    public async Task HandleAsync(BookingConfirmed domainEvent, CancellationToken cancellationToken)
    {
        var booking = await loader.LoadAsync(domainEvent.BookingId, cancellationToken)
                      ?? throw new InvalidOperationException($"Booking {domainEvent.BookingId} doesn't exist.");

        if (booking.ContactEmail is null)
        {
            logger.LogInformation("Booking {BookingNo} is confirmed, but has no email address - no confirmation sent.", booking.BookingNo);
            return;
        }

        EmailAttachment[] attachments =
        [
            new($"Voucher-{booking.BookingNo}.pdf", BookingDocumentFile.ContentType, renderer.RenderVoucher(booking)),
            new($"Invoice-{booking.BookingNo}.pdf", BookingDocumentFile.ContentType, renderer.RenderInvoice(booking))
        ];

        await emails.SendAsync(
            new EmailMessage(booking.ContactEmail, $"Booking confirmed - {booking.BookingNo} - {booking.TripTitle}", Body(booking), attachments),
            cancellationToken);
    }

    // HtmlEncode everything a person typed (names, the package title): a
    // name like "<script>" must arrive as text, not as part of the email's HTML.
    private static string Body(BookingDocumentData booking)
    {
        var people = booking.Travellers.Count;
        var dates = booking.StartDate == booking.EndDate
            ? Date(booking.StartDate)
            : $"{Date(booking.StartDate)} – {Date(booking.EndDate)}";
        var nights = booking.Nights == 0 ? "day trip" : booking.Nights == 1 ? "1 night" : $"{booking.Nights} nights";

        return $"""
            <p>Hi {WebUtility.HtmlEncode(booking.ContactName)},</p>
            <p>Your payment was received and your booking is <strong>confirmed</strong>.</p>
            <table cellpadding="4">
              <tr><td>Booking</td><td><strong>{booking.BookingNo}</strong></td></tr>
              <tr><td>Trip</td><td>{WebUtility.HtmlEncode(booking.TripTitle)}</td></tr>
              <tr><td>Dates</td><td>{dates} ({nights})</td></tr>
              <tr><td>Travellers</td><td>{people}</td></tr>
              <tr><td>Paid</td><td>{Money(booking.PaidAmount, booking.Currency)}</td></tr>
            </table>
            <p>Your <strong>e-voucher</strong> and <strong>invoice</strong> are attached. Please keep the voucher with you on the trip.</p>
            <p>You can also see this booking any time under <em>My bookings</em> on our website.</p>
            <p>Have a great trip!<br>The Ghuri team</p>
            """;
    }

    private static string Date(DateOnly date) => date.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>"৳34,000" - paisa only when there are any.</summary>
    internal static string Money(decimal amount, string currency)
    {
        var symbol = currency == "BDT" ? "৳" : currency + " ";
        var format = amount == decimal.Truncate(amount) ? "#,0" : "#,0.00";
        return symbol + amount.ToString(format, CultureInfo.InvariantCulture);
    }
}

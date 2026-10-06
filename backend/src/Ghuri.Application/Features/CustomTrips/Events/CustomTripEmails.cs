using System.Globalization;
using System.Net;
using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Booking;
using Ghuri.Domain.Entities.Notify;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ghuri.Application.Features.CustomTrips.Events;

// The custom-trip messages (17-day plan, Day 13), sent by the outbox job a
// few seconds after the change was saved:
//   CustomTripSubmitted → "New request" to the staff inbox + "We got it" to the customer
//   CustomTripQuoted    → "Your quote" to the customer + an SMS-ready text (queued, Phase 2 sends it)
//   CustomTripRejected  → "Sorry, we can't" to the customer
// Everything a person typed is HtmlEncoded: a name like "<script>" arrives as text.

/// <summary>Shared by the three handlers: load the trip, format money and dates the same way.</summary>
internal sealed class CustomTripMessageFormat(IReadDbContext db, CustomTripReader reader)
{
    public async Task<CustomTripDto> LoadAsync(Guid tripId, CancellationToken cancellationToken)
    {
        var tripNo = await db.CustomTrips.Where(t => t.Id == tripId).Select(t => t.TripNo).FirstOrDefaultAsync(cancellationToken)
                     ?? throw new InvalidOperationException($"Custom trip {tripId} doesn't exist.");
        return (await reader.LoadAsync(tripNo, customerId: null, cancellationToken))!.Value.Trip;
    }

    public static string Route(CustomTripDto trip) => string.Join(" → ", trip.Legs.Select(l => l.DestinationName));

    public static string Dates(CustomTripDto trip) =>
        $"{Date(trip.StartDate)} – {Date(trip.EndDate)} ({trip.TotalNights} night{(trip.TotalNights == 1 ? "" : "s")})";

    public static string People(CustomTripDto trip) =>
        $"{trip.Adults} adult(s){(trip.Children > 0 ? $", {trip.Children} child(ren)" : "")}{(trip.Infants > 0 ? $", {trip.Infants} infant(s)" : "")}";

    public static string Date(DateOnly date) => date.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>Staff and customers think in Dhaka time: "9 Oct 2026, 2:30 PM".</summary>
    public static string DhakaTime(DateTime utc) =>
        (utc + AgencyTime.UtcOffset).ToString("d MMM yyyy, h:mm tt", CultureInfo.InvariantCulture);

    public static string Money(decimal amount, string currency) =>
        (currency == "BDT" ? "৳" : currency + " ") + amount.ToString(amount == decimal.Truncate(amount) ? "#,0" : "#,0.00", CultureInfo.InvariantCulture);

    public static string Html(string? text) => WebUtility.HtmlEncode(text ?? string.Empty).Replace("\n", "<br>");

    /// <summary>The stops as an HTML list: "Cox's Bazar - 20 Dec 2026 to 23 Dec 2026 (3 nights), then by bus".</summary>
    public static string LegsHtml(CustomTripDto trip) =>
        "<ol>" + string.Concat(trip.Legs.Select(l =>
            $"<li>{Html(l.DestinationName)} - {Date(l.CheckInDate)} to {Date(l.CheckOutDate)} ({l.Nights} night{(l.Nights == 1 ? "" : "s")})" +
            (l.TransferToNext == TransferMode.None ? "" : $", then by {Transfer(l.TransferToNext)}") + "</li>")) + "</ol>";

    private static string Transfer(TransferMode mode) => mode switch
    {
        TransferMode.PrivateCar => "private car",
        _ => mode.ToString().ToLowerInvariant()
    };
}

/// <summary>A new request: alert the staff inbox (Agency:BookingsEmail) and reassure the customer.</summary>
internal sealed class SendCustomTripSubmittedEmails(
    CustomTripMessageFormat format,
    IEmailSender emails,
    IOptions<SiteOptions> site,
    IOptions<StaffAlertOptions> staff,
    ILogger<SendCustomTripSubmittedEmails> logger) : IDomainEventHandler<CustomTripSubmitted>
{
    public async Task HandleAsync(CustomTripSubmitted domainEvent, CancellationToken cancellationToken)
    {
        var trip = await format.LoadAsync(domainEvent.CustomTripId, cancellationToken);
        var route = CustomTripMessageFormat.Route(trip);
        var summary = $"""
            <table cellpadding="4">
              <tr><td>Request</td><td><strong>{trip.TripNo}</strong></td></tr>
              <tr><td>Route</td><td>{CustomTripMessageFormat.Html(route)}</td></tr>
              <tr><td>Dates</td><td>{CustomTripMessageFormat.Dates(trip)}</td></tr>
              <tr><td>Travellers</td><td>{CustomTripMessageFormat.People(trip)}</td></tr>
              <tr><td>Hotels</td><td>{trip.HotelLevel}</td></tr>
              {(trip.BudgetPerPerson is { } budget ? $"<tr><td>Budget</td><td>{CustomTripMessageFormat.Money(budget, "BDT")} per person</td></tr>" : "")}
            </table>
            {CustomTripMessageFormat.LegsHtml(trip)}
            {(trip.Notes is null ? "" : $"<p><em>Notes:</em> {CustomTripMessageFormat.Html(trip.Notes)}</p>")}
            """;

        // Staff first: if the staff inbox fails, the outbox retries - and the customer's mail goes with it.
        if (!string.IsNullOrWhiteSpace(staff.Value.BookingsEmail))
        {
            await emails.SendAsync(new EmailMessage(
                staff.Value.BookingsEmail,
                $"New custom trip request {trip.TripNo} - {route}",
                $"""
                <p>A new custom trip request is waiting for a quote (target: within 24 hours).</p>
                <p><strong>{CustomTripMessageFormat.Html(trip.ContactName)}</strong> · {trip.ContactPhone}{(trip.ContactEmail is null ? "" : $" · {CustomTripMessageFormat.Html(trip.ContactEmail)}")}</p>
                {summary}
                <p><a href="{WebUtility.HtmlEncode(site.Value.Link($"admin/custom-trips/{trip.TripNo}"))}">Open it in the admin panel</a></p>
                """), cancellationToken);
        }
        else
        {
            logger.LogWarning("Custom trip {TripNo} submitted, but Agency:BookingsEmail is empty - no staff alert sent.", trip.TripNo);
        }

        if (trip.ContactEmail is not null)
        {
            await emails.SendAsync(new EmailMessage(
                trip.ContactEmail,
                $"We received your trip request {trip.TripNo}",
                $"""
                <p>Hi {CustomTripMessageFormat.Html(trip.ContactName)},</p>
                <p>Thank you - we received your custom trip request. Our team will send you a price <strong>within 24 hours</strong>.</p>
                {summary}
                <p><a href="{WebUtility.HtmlEncode(site.Value.Link($"account/trips/{trip.TripNo}"))}">See your request</a></p>
                <p>The Ghuri team</p>
                """), cancellationToken);
        }
    }
}

/// <summary>A quote (or a new version of it): email the customer, and queue a short SMS for Phase 2.</summary>
internal sealed class SendCustomTripQuotedMessages(
    CustomTripMessageFormat format,
    IEmailSender emails,
    INotificationRepository notifications,
    IReadDbContext db,
    IOptions<SiteOptions> site,
    TimeProvider clock) : IDomainEventHandler<CustomTripQuoted>
{
    public const string SmsTemplateCode = "CUSTOM_TRIP_QUOTED";

    public async Task HandleAsync(CustomTripQuoted domainEvent, CancellationToken cancellationToken)
    {
        var trip = await format.LoadAsync(domainEvent.CustomTripId, cancellationToken);
        var quote = trip.Quote ?? throw new InvalidOperationException($"Custom trip {trip.TripNo} has no quote.");
        if (quote.Version != domainEvent.QuoteVersion)
            return; // a newer quote was sent meanwhile - its own event emails that one

        var link = site.Value.Link($"account/trips/{trip.TripNo}");
        var total = CustomTripMessageFormat.Money(quote.Total, quote.Currency);
        var validUntil = CustomTripMessageFormat.DhakaTime(quote.ExpiresAtUtc);

        if (trip.ContactEmail is not null)
        {
            var lines = string.Concat(quote.Lines.Select(l =>
                $"<tr><td>{l.Category}</td><td>{CustomTripMessageFormat.Html(l.Description)}</td><td align=\"right\">{CustomTripMessageFormat.Money(l.Amount, quote.Currency)}</td></tr>"));
            await emails.SendAsync(new EmailMessage(
                trip.ContactEmail,
                $"{(quote.Version > 1 ? "Updated quote" : "Your quote")} for trip {trip.TripNo}: {total}",
                $"""
                <p>Hi {CustomTripMessageFormat.Html(trip.ContactName)},</p>
                <p>{(quote.Version > 1 ? "Here is the <strong>updated</strong> price" : "Here is the price")} for your trip
                   {CustomTripMessageFormat.Html(CustomTripMessageFormat.Route(trip))}, {CustomTripMessageFormat.Dates(trip)}.</p>
                <p>{CustomTripMessageFormat.Html(quote.Itinerary)}</p>
                <table cellpadding="4">{lines}
                  <tr><td colspan="2"><strong>Total</strong></td><td align="right"><strong>{total}</strong></td></tr>
                </table>
                <p>This offer is valid until <strong>{validUntil}</strong> (Dhaka time).</p>
                <p><a href="{WebUtility.HtmlEncode(link)}">See the quote and accept it</a></p>
                <p>The Ghuri team</p>
                """), cancellationToken);
        }

        // "SMS-ready": stored now, sent when SMS arrives (Phase 2). Short - one SMS is 160 characters.
        var userId = await db.CustomTrips.Where(t => t.TripNo == trip.TripNo).Select(t => t.CustomerId).SingleAsync(cancellationToken);
        notifications.Add(Notification.Queue(
            NotificationChannel.Sms, trip.ContactPhone, SmsTemplateCode,
            $"Ghuri: your trip quote {trip.TripNo} is {total.Replace("৳", "Tk ")}, valid till {validUntil}. {link}",
            clock.GetUtcNow().UtcDateTime, userId));
    }
}

/// <summary>Staff can't do the trip: tell the customer why.</summary>
internal sealed class SendCustomTripRejectedEmail(CustomTripMessageFormat format, IEmailSender emails) : IDomainEventHandler<CustomTripRejected>
{
    public async Task HandleAsync(CustomTripRejected domainEvent, CancellationToken cancellationToken)
    {
        var trip = await format.LoadAsync(domainEvent.CustomTripId, cancellationToken);
        if (trip.ContactEmail is null)
            return;

        await emails.SendAsync(new EmailMessage(
            trip.ContactEmail,
            $"About your trip request {trip.TripNo}",
            $"""
            <p>Hi {CustomTripMessageFormat.Html(trip.ContactName)},</p>
            <p>We're sorry - we can't arrange your trip {CustomTripMessageFormat.Html(CustomTripMessageFormat.Route(trip))}
               on {CustomTripMessageFormat.Dates(trip)}.</p>
            <p><em>{CustomTripMessageFormat.Html(trip.Timeline.RejectReason)}</em></p>
            <p>You're welcome to send a new request with other dates or destinations, or call us - we're happy to help.</p>
            <p>The Ghuri team</p>
            """), cancellationToken);
    }
}

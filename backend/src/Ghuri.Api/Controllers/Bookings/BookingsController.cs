using Ghuri.Api.ErrorHandling;
using Ghuri.Api.RateLimiting;
using Ghuri.Application.Features.Booking.Commands.CancelMyBooking;
using Ghuri.Application.Features.Booking.Commands.CreateBooking;
using Ghuri.Application.Features.Booking.Queries.GetMyBooking;
using Ghuri.Application.Features.Booking.Queries.GetMyBookingDocument;
using Ghuri.Application.Features.Booking.Queries.GetMyBookings;
using Ghuri.Application.Features.Payments.Commands.InitiatePayment;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ghuri.Api.Controllers.Bookings;

/// <summary>
/// The customer's own bookings (17-day plan, Day 8). Login required - any
/// logged-in user may book; the booking belongs to whoever is logged in.
/// </summary>
[ApiController]
[Route("api/v1/bookings")]
[Authorize]
public sealed class BookingsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Book a trip, held 20 minutes for payment. Needs an "Idempotency-Key"
    /// header - a new random id (e.g. a UUID) per checkout attempt. Sending
    /// the same request again with the same key returns the SAME booking.
    /// 201 = { bookingId, bookingNo, totalAmount, currency, holdExpiresAtUtc }.
    /// </summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.CustomerWrites)]
    public async Task<IActionResult> Create(
        CreateBookingCommand command,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { IdempotencyKey = idempotencyKey }, cancellationToken);
        return result.IsSuccess
            ? Created($"/api/v1/bookings/{result.Value.BookingNo}", result.Value)
            : ResultExtensions.ToProblem(result.Error);
    }

    /// <summary>
    /// Pay for one of your own bookings: creates a payment and returns the
    /// gateway page to send the browser to - { paymentNo, paymentPageUrl }.
    /// 409 booking_hold_ended once the 20 minutes are over.
    /// </summary>
    [HttpPost("{bookingNo}/payments")]
    [EnableRateLimiting(RateLimitPolicies.CustomerWrites)]
    public async Task<IActionResult> Pay(string bookingNo, CancellationToken cancellationToken) =>
        (await sender.Send(new InitiatePaymentCommand(bookingNo), cancellationToken)).ToActionResult();

    /// <summary>All your bookings, newest first ("My bookings").</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        (await sender.Send(new GetMyBookingsQuery(), cancellationToken)).ToActionResult();

    /// <summary>
    /// One of your own bookings by its number, e.g. /api/v1/bookings/TB100001. Someone else's is 404.
    /// "cancellation" says whether it can be cancelled now and how much would come back.
    /// </summary>
    [HttpGet("{bookingNo}")]
    public async Task<IActionResult> Get(string bookingNo, CancellationToken cancellationToken) =>
        (await sender.Send(new GetMyBookingQuery(bookingNo), cancellationToken)).ToActionResult();

    /// <summary>
    /// Cancel one of your own bookings - body { "reason": "..." } (optional).
    /// 200 = { bookingNo, refundPercent, refundAmount, refundNo }.
    /// 409 booking_not_cancellable once expired, cancelled or the trip has started.
    /// </summary>
    [HttpPost("{bookingNo}/cancel")]
    [EnableRateLimiting(RateLimitPolicies.CustomerWrites)]
    public async Task<IActionResult> Cancel(string bookingNo, CancelBookingRequest? request, CancellationToken cancellationToken) =>
        (await sender.Send(new CancelMyBookingCommand(bookingNo, request?.Reason), cancellationToken)).ToActionResult();

    /// <summary>The invoice as a PDF download ("Invoice-TB100001.pdf"). 409 document_not_available until something is paid.</summary>
    [HttpGet("{bookingNo}/invoice")]
    public Task<IActionResult> Invoice(string bookingNo, CancellationToken cancellationToken) =>
        DocumentAsync(bookingNo, BookingDocumentKind.Invoice, cancellationToken);

    /// <summary>The e-voucher as a PDF download ("Voucher-TB100001.pdf"). 409 document_not_available unless the booking is confirmed.</summary>
    [HttpGet("{bookingNo}/voucher")]
    public Task<IActionResult> Voucher(string bookingNo, CancellationToken cancellationToken) =>
        DocumentAsync(bookingNo, BookingDocumentKind.Voucher, cancellationToken);

    private async Task<IActionResult> DocumentAsync(string bookingNo, BookingDocumentKind kind, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyBookingDocumentQuery(bookingNo, kind), cancellationToken);
        return result.IsSuccess
            ? File(result.Value.Content, BookingDocumentFile.ContentType, result.Value.FileName)
            : ResultExtensions.ToProblem(result.Error);
    }
}

/// <summary>Body of POST /api/v1/bookings/{bookingNo}/cancel - the number comes from the URL.</summary>
public sealed record CancelBookingRequest(string? Reason);

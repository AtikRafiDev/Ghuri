using Ghuri.Api.Authentication;
using Ghuri.Api.ErrorHandling;
using Ghuri.Application.Features.Booking.Commands.CancelBookingByAgency;
using Ghuri.Application.Features.Booking.Queries.GetBookingDocumentForAdmin;
using Ghuri.Application.Features.Booking.Queries.GetBookingForAdmin;
using Ghuri.Application.Features.Booking.Queries.GetMyBookingDocument;
using Ghuri.Application.Features.Booking.Queries.SearchBookings;
using Ghuri.Application.Features.Payments.Commands.RecordManualPayment;
using Ghuri.Domain.Enums;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ghuri.Api.Controllers.Admin;

/// <summary>
/// Staff work with bookings (17-day plan, Day 12). Every staff member can
/// look; cancelling needs the CancelBookings policy, taking money ManageMoney.
/// </summary>
[ApiController]
[Route("api/v1/admin/bookings")]
[Authorize(Policy = Policies.AdminArea)]
[Authorize(Policy = Policies.ViewBookings)]
public sealed class AdminBookingsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Search / filter / page, newest first.
    /// ?search=TB1001|name|01711000000&amp;status=1..6&amp;type=1..3&amp;tripFrom=2026-12-01&amp;tripTo=2026-12-31&amp;page=1&amp;pageSize=20
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] BookingStatus? status, [FromQuery] BookingType? type,
        [FromQuery] DateOnly? tripFrom, [FromQuery] DateOnly? tripTo,
        CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        (await sender.Send(new SearchBookingsQuery(search, status, type, tripFrom, tripTo, page, pageSize), cancellationToken))
        .ToActionResult();

    /// <summary>One booking with its travellers, history, payments and refunds.</summary>
    [HttpGet("{bookingNo}")]
    public async Task<IActionResult> Get(string bookingNo, CancellationToken cancellationToken) =>
        (await sender.Send(new GetBookingForAdminQuery(bookingNo), cancellationToken)).ToActionResult();

    /// <summary>
    /// Cancel for the agency - body { "reason": "..." } (required). Everything paid is refunded in full.
    /// 200 = { bookingNo, refundAmount, refundNo }.
    /// </summary>
    [HttpPost("{bookingNo}/cancel")]
    [Authorize(Policy = Policies.CancelBookings)]
    public async Task<IActionResult> Cancel(string bookingNo, AgencyCancelRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new CancelBookingByAgencyCommand(bookingNo, request.Reason), cancellationToken)).ToActionResult();

    /// <summary>
    /// Money paid straight to the agency - body { amount, method: 1 cash · 2 bank transfer · 3 bKash · 4 Nagad · 5 Rocket · 6 card · 7 other, reference }.
    /// Confirms the booking (the voucher email follows). 200 = { paymentNo, bookingNo, bookingStatus }.
    /// </summary>
    [HttpPost("{bookingNo}/payments")]
    [Authorize(Policy = Policies.ManageMoney)]
    public async Task<IActionResult> RecordPayment(string bookingNo, ManualPaymentRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new RecordManualPaymentCommand(bookingNo, request.Amount, request.Method, request.Reference), cancellationToken))
        .ToActionResult();

    /// <summary>The invoice PDF - the same file the customer downloads.</summary>
    [HttpGet("{bookingNo}/invoice")]
    public Task<IActionResult> Invoice(string bookingNo, CancellationToken cancellationToken) =>
        DocumentAsync(bookingNo, BookingDocumentKind.Invoice, cancellationToken);

    /// <summary>The e-voucher PDF - e.g. to send again by WhatsApp. Only for a confirmed booking.</summary>
    [HttpGet("{bookingNo}/voucher")]
    public Task<IActionResult> Voucher(string bookingNo, CancellationToken cancellationToken) =>
        DocumentAsync(bookingNo, BookingDocumentKind.Voucher, cancellationToken);

    private async Task<IActionResult> DocumentAsync(string bookingNo, BookingDocumentKind kind, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBookingDocumentForAdminQuery(bookingNo, kind), cancellationToken);
        return result.IsSuccess
            ? File(result.Value.Content, BookingDocumentFile.ContentType, result.Value.FileName)
            : ResultExtensions.ToProblem(result.Error);
    }
}

/// <summary>Body of POST admin/bookings/{bookingNo}/cancel.</summary>
public sealed record AgencyCancelRequest(string Reason);

/// <summary>Body of POST admin/bookings/{bookingNo}/payments.</summary>
public sealed record ManualPaymentRequest(decimal Amount, ManualPaymentMethod Method, string? Reference);

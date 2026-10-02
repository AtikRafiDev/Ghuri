using Ghuri.Api.ErrorHandling;
using Ghuri.Application.Features.Booking.Commands.CreateBooking;
using Ghuri.Application.Features.Booking.Queries.GetMyBooking;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    /// <summary>One of your own bookings by its number, e.g. /api/v1/bookings/TB100001. Someone else's is 404.</summary>
    [HttpGet("{bookingNo}")]
    public async Task<IActionResult> Get(string bookingNo, CancellationToken cancellationToken) =>
        (await sender.Send(new GetMyBookingQuery(bookingNo), cancellationToken)).ToActionResult();
}

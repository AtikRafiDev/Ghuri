using Ghuri.Api.Authentication;
using Ghuri.Api.ErrorHandling;
using Ghuri.Application.Features.CustomTrips.Commands.QuoteCustomTrip;
using Ghuri.Application.Features.CustomTrips.Commands.RejectCustomTrip;
using Ghuri.Application.Features.CustomTrips.Queries;
using Ghuri.Domain.Enums;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ghuri.Api.Controllers.Admin;

/// <summary>
/// Staff work through custom trip requests (17-day plan, Day 13). Every
/// staff member can look; quoting and rejecting need the QuoteTrips policy.
/// </summary>
[ApiController]
[Route("api/v1/admin/custom-trips")]
[Authorize(Policy = Policies.AdminArea)]
public sealed class AdminCustomTripsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// The queue. ?status=1 (waiting - oldest first) | 2 quoted | 3 accepted | 4 paid | 5 rejected | 6 expired | 7 cancelled
    /// &amp;search=CT1001|name|mobile&amp;page=1&amp;pageSize=20. No status = all, newest first.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Queue(
        [FromQuery] CustomTripStatus? status, [FromQuery] string? search,
        CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        (await sender.Send(new GetCustomTripQueueQuery(status, search, page, pageSize), cancellationToken)).ToActionResult();

    /// <summary>One request: the trip, its quote, the customer and who quoted it.</summary>
    [HttpGet("{tripNo}")]
    public async Task<IActionResult> Get(string tripNo, CancellationToken cancellationToken) =>
        (await sender.Send(new GetCustomTripForAdminQuery(tripNo), cancellationToken)).ToActionResult();

    /// <summary>
    /// Send (or replace) the quote: { itinerary, lines: [{ category: 1 hotel · 2 transport · 3 meals · 4 guide · 5 activities · 6 other,
    /// description, amount }], validDays (default 3) }. 200 = { tripNo, quoteVersion, total, expiresAtUtc }. The customer is emailed.
    /// </summary>
    [HttpPost("{tripNo}/quote")]
    [Authorize(Policy = Policies.QuoteTrips)]
    public async Task<IActionResult> Quote(string tripNo, QuoteRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new QuoteCustomTripCommand(tripNo, request.Itinerary, request.Lines, request.ValidDays), cancellationToken))
        .ToActionResult();

    /// <summary>Can't do it - body { "reason": "..." } (the customer is emailed this).</summary>
    [HttpPost("{tripNo}/reject")]
    [Authorize(Policy = Policies.QuoteTrips)]
    public async Task<IActionResult> Reject(string tripNo, RejectTripRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new RejectCustomTripCommand(tripNo, request.Reason), cancellationToken)).ToActionResult();
}

/// <summary>Body of POST admin/custom-trips/{tripNo}/quote - the number comes from the URL.</summary>
public sealed record QuoteRequest(string Itinerary, IReadOnlyList<QuoteCustomTripLine> Lines, int? ValidDays);

/// <summary>Body of POST admin/custom-trips/{tripNo}/reject.</summary>
public sealed record RejectTripRequest(string Reason);

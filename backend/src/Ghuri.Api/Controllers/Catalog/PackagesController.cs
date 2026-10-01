using Ghuri.Api.ErrorHandling;
using Ghuri.Application.Features.Booking.Queries.GetBookingQuote;
using Ghuri.Application.Features.Catalog.Queries.GetDepartureAvailability;
using Ghuri.Application.Features.Catalog.Queries.GetPackageDetails;
using Ghuri.Application.Features.Catalog.Queries.SearchPackages;
using Ghuri.Domain.Enums;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Ghuri.Api.Controllers.Catalog;

/// <summary>
/// The public package pages (blueprint section 11, "Public catalogue") - no
/// login needed. Only published packages exist here; a draft or archived
/// one is a 404.
/// </summary>
[ApiController]
[Route("api/v1/packages")]
public sealed class PackagesController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Search: ?q=&amp;destination={slug}&amp;category={slug}&amp;minPrice=&amp;maxPrice=
    /// &amp;mode=FixedDepartures|FlexibleStay&amp;sort=Recommended|PriceLow|PriceHigh|Newest&amp;page=1&amp;pageSize=12
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? q, [FromQuery] string? destination, [FromQuery] string? category,
        [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] PricingMode? mode,
        CancellationToken cancellationToken,
        [FromQuery] PackageSort sort = PackageSort.Recommended, [FromQuery] int page = 1, [FromQuery] int pageSize = 12) =>
        (await sender.Send(
            new SearchPackagesQuery(q, destination, category, minPrice, maxPrice, mode, sort, page, pageSize),
            cancellationToken)).ToActionResult();

    /// <summary>One package page by its URL name, e.g. /api/v1/packages/cox-s-bazar-3-days.</summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken) =>
        (await sender.Send(new GetPackageDetailsQuery(slug), cancellationToken)).ToActionResult();

    /// <summary>The dates that can still be booked (fixed packages; empty for a flexible stay).</summary>
    [HttpGet("{slug}/departures")]
    public async Task<IActionResult> Departures(string slug, CancellationToken cancellationToken) =>
        (await sender.Send(new GetDepartureAvailabilityQuery(slug), cancellationToken)).ToActionResult();

    /// <summary>
    /// What a trip would cost - reserves nothing. Fixed: ?departureId=&amp;adults=2&amp;children=1&amp;infants=0&amp;singleRooms=0.
    /// Flexible: ?startDate=2026-12-20&amp;nights=3&amp;adults=2. A GET because it changes nothing: it can be cached and shared as a link.
    /// </summary>
    [HttpGet("{slug}/quote")]
    public async Task<IActionResult> Quote(
        string slug, [FromQuery] Guid? departureId, [FromQuery] DateOnly? startDate, [FromQuery] int? nights,
        CancellationToken cancellationToken,
        [FromQuery] int adults = 1, [FromQuery] int children = 0, [FromQuery] int infants = 0, [FromQuery] int singleRooms = 0) =>
        (await sender.Send(
            new GetBookingQuoteQuery(slug, departureId, startDate, nights, adults, children, infants, singleRooms),
            cancellationToken)).ToActionResult();
}

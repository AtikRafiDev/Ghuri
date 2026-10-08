using Ghuri.Api.Authentication;
using Ghuri.Api.ErrorHandling;
using Ghuri.Application.Features.Catalog.Commands.ArchivePackage;
using Ghuri.Application.Features.Catalog.Commands.CreateDeparture;
using Ghuri.Application.Features.Catalog.Commands.CreatePackage;
using Ghuri.Application.Features.Catalog.Commands.PublishPackage;
using Ghuri.Application.Features.Catalog.Commands.SavePackageItinerary;
using Ghuri.Application.Features.Catalog.Commands.SetPackageImages;
using Ghuri.Application.Features.Catalog.Commands.UpdatePackage;
using Ghuri.Application.Features.Catalog.Queries.GetAdminPackage;
using Ghuri.Application.Features.Catalog.Queries.GetAdminPackages;
using Ghuri.Application.Features.Catalog.Queries.GetPackageDepartures;
using Ghuri.Domain.Enums;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ghuri.Api.Controllers.Admin;

/// <summary>
/// Tour package management (blueprint: GET|POST admin/packages · GET|PUT
/// admin/packages/{id} · PUT .../images · PUT .../itinerary · POST
/// .../publish · POST .../archive).
/// </summary>
/// <remarks>
/// Photos and the itinerary have their own endpoints because they're
/// separate tabs of the edit page, saved on their own.
/// </remarks>
[ApiController]
[Route("api/v1/admin/packages")]
[Authorize(Policy = Policies.AdminArea)]
[Authorize(Policy = Policies.ManageCatalogue)]
public sealed class AdminPackagesController(ISender sender) : ControllerBase
{
    /// <summary>Search / filter / page, newest first. ?search=&amp;destinationId=&amp;status=1|2|3&amp;pricingMode=1|2&amp;page=1&amp;pageSize=20</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] Guid? destinationId,
        [FromQuery] PackageStatus? status, [FromQuery] PricingMode? pricingMode,
        CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        (await sender.Send(new GetAdminPackagesQuery(search, destinationId, status, pricingMode, page, pageSize), cancellationToken))
        .ToActionResult();

    /// <summary>One package with photos, itinerary and what still stops it being published.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        (await sender.Send(new GetAdminPackageQuery(id), cancellationToken)).ToActionResult();

    /// <summary>Add a package as a Draft. Slug may be left empty - it's made from the title.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreatePackageCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Created((string?)null, new CreatedResponse(result.Value))
            : ResultExtensions.ToProblem(result.Error);
    }

    /// <summary>Replace the form fields. The id in the URL wins over any id in the body.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdatePackageCommand command, CancellationToken cancellationToken) =>
        (await sender.Send(command with { Id = id }, cancellationToken)).ToActionResult();

    /// <summary>Replace the gallery: { "imageFileIds": [...] } in display order, the first is the cover.</summary>
    [HttpPut("{id:guid}/images")]
    public async Task<IActionResult> SetImages(Guid id, SetPackageImagesCommand command, CancellationToken cancellationToken) =>
        (await sender.Send(command with { Id = id }, cancellationToken)).ToActionResult();

    /// <summary>Replace the itinerary: { "days": [{ title, description, meals, accommodation }] } - the first is Day 1.</summary>
    [HttpPut("{id:guid}/itinerary")]
    public async Task<IActionResult> SaveItinerary(Guid id, SavePackageItineraryCommand command, CancellationToken cancellationToken) =>
        (await sender.Send(command with { Id = id }, cancellationToken)).ToActionResult();

    /// <summary>Put it on the public site. 409 package_not_publishable lists everything still missing.</summary>
    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken) =>
        (await sender.Send(new PublishPackageCommand(id), cancellationToken)).ToActionResult();

    /// <summary>The package's departures, earliest first (fixed-departure packages only have any).</summary>
    [HttpGet("{id:guid}/departures")]
    public async Task<IActionResult> Departures(Guid id, CancellationToken cancellationToken) =>
        (await sender.Send(new GetPackageDeparturesQuery(id), cancellationToken)).ToActionResult();

    /// <summary>Add a departure date. EndDate is worked out from the package's duration. 201 = { id }.</summary>
    [HttpPost("{id:guid}/departures")]
    public async Task<IActionResult> AddDeparture(Guid id, CreateDepartureCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { PackageId = id }, cancellationToken);
        return result.IsSuccess
            ? Created((string?)null, new CreatedResponse(result.Value))
            : ResultExtensions.ToProblem(result.Error);
    }

    /// <summary>Take it off the public site (it can be published again later).</summary>
    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken) =>
        (await sender.Send(new ArchivePackageCommand(id), cancellationToken)).ToActionResult();
}

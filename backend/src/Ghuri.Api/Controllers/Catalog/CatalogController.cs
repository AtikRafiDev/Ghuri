using Ghuri.Api.ErrorHandling;
using Ghuri.Application.Features.Catalog;
using Ghuri.Application.Features.Catalog.Queries.GetCategories;
using Ghuri.Application.Features.Catalog.Queries.GetCountries;
using Ghuri.Application.Features.Catalog.Queries.GetDestinations;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Ghuri.Api.Controllers.Catalog;

/// <summary>Public catalogue lists (blueprint section 11, "Public catalogue") - no login needed.</summary>
[ApiController]
[Route("api/v1")]
public sealed class CatalogController(ISender sender) : ControllerBase
{
    /// <summary>Destinations. ?scope=national|international, ?featured=true for the home page ones.</summary>
    [HttpGet("destinations")]
    public async Task<IActionResult> Destinations(
        [FromQuery] DestinationScope? scope, [FromQuery] bool featured, CancellationToken cancellationToken) =>
        (await sender.Send(new GetDestinationsQuery(scope, featured), cancellationToken)).ToActionResult();

    /// <summary>Tour categories (Beach, Hill, Honeymoon...).</summary>
    [HttpGet("categories")]
    public async Task<IActionResult> Categories(CancellationToken cancellationToken) =>
        (await sender.Send(new GetCategoriesQuery(), cancellationToken)).ToActionResult();

    /// <summary>All countries A-Z, for dropdowns.</summary>
    [HttpGet("countries")]
    public async Task<IActionResult> Countries(CancellationToken cancellationToken) =>
        (await sender.Send(new GetCountriesQuery(), cancellationToken)).ToActionResult();
}

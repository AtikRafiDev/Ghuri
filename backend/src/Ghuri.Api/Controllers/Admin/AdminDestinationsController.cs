using Ghuri.Api.Authentication;
using Ghuri.Api.ErrorHandling;
using Ghuri.Application.Features.Catalog;
using Ghuri.Application.Features.Catalog.Commands.CreateDestination;
using Ghuri.Application.Features.Catalog.Commands.DeleteDestination;
using Ghuri.Application.Features.Catalog.Commands.UpdateDestination;
using Ghuri.Application.Features.Catalog.Queries.GetAdminDestinations;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ghuri.Api.Controllers.Admin;

/// <summary>Destination management (blueprint: GET|POST admin/destinations · PUT|DELETE admin/destinations/{id}).</summary>
[ApiController]
[Route("api/v1/admin/destinations")]
[Authorize(Policy = Policies.AdminArea)]
[Authorize(Policy = Policies.ManageCatalogue)]
public sealed class AdminDestinationsController(ISender sender) : ControllerBase
{
    /// <summary>Search / filter / page. ?search=&amp;countryId=&amp;scope=national|international&amp;page=1&amp;pageSize=20</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] short? countryId, [FromQuery] DestinationScope? scope,
        CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        (await sender.Send(new GetAdminDestinationsQuery(search, countryId, scope, page, pageSize), cancellationToken))
        .ToActionResult();

    /// <summary>Add a destination. Slug may be left empty - it's made from the name.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateDestinationCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Created((string?)null, new CreatedResponse(result.Value))
            : ResultExtensions.ToProblem(result.Error);
    }

    /// <summary>Replace a destination's fields. The id in the URL wins over any id in the body.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateDestinationCommand command, CancellationToken cancellationToken) =>
        (await sender.Send(command with { Id = id }, cancellationToken)).ToActionResult();

    /// <summary>Soft-delete. Refused (409) while tour packages still use it.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        (await sender.Send(new DeleteDestinationCommand(id), cancellationToken)).ToActionResult();
}

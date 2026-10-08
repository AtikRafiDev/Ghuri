using Ghuri.Api.Authentication;
using Ghuri.Api.ErrorHandling;
using Ghuri.Application.Features.Catalog.Commands.CloseDeparture;
using Ghuri.Application.Features.Catalog.Commands.UpdateDeparture;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ghuri.Api.Controllers.Admin;

/// <summary>
/// One departure, by its own id (blueprint: PUT admin/departures/{id} ·
/// POST admin/departures/{id}/close). Listing and adding live under the
/// package: GET|POST admin/packages/{id}/departures.
/// </summary>
[ApiController]
[Route("api/v1/admin/departures")]
[Authorize(Policy = Policies.AdminArea)]
[Authorize(Policy = Policies.ManageCatalogue)]
public sealed class AdminDeparturesController(ISender sender) : ControllerBase
{
    /// <summary>Replace date, prices and seats. 409 if booked seats would be lost or moved.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateDepartureCommand command, CancellationToken cancellationToken) =>
        (await sender.Send(command with { Id = id }, cancellationToken)).ToActionResult();

    /// <summary>Stop new bookings. Existing bookings keep their seats.</summary>
    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken) =>
        (await sender.Send(new CloseDepartureCommand(id), cancellationToken)).ToActionResult();
}

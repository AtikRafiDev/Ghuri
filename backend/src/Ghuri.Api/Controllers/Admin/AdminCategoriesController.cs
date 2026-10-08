using Ghuri.Api.Authentication;
using Ghuri.Api.ErrorHandling;
using Ghuri.Application.Features.Catalog.Commands.CreateCategory;
using Ghuri.Application.Features.Catalog.Commands.DeleteCategory;
using Ghuri.Application.Features.Catalog.Commands.UpdateCategory;
using Ghuri.Application.Features.Catalog.Queries.GetAdminCategories;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ghuri.Api.Controllers.Admin;

/// <summary>Category management (blueprint: GET|POST admin/categories · PUT|DELETE admin/categories/{id}).</summary>
[ApiController]
[Route("api/v1/admin/categories")]
[Authorize(Policy = Policies.AdminArea)]
[Authorize(Policy = Policies.ManageCatalogue)]
public sealed class AdminCategoriesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        (await sender.Send(new GetAdminCategoriesQuery(), cancellationToken)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Created((string?)null, new CreatedResponse(result.Value))
            : ResultExtensions.ToProblem(result.Error);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCategoryCommand command, CancellationToken cancellationToken) =>
        (await sender.Send(command with { Id = id }, cancellationToken)).ToActionResult();

    /// <summary>Soft-delete. Refused (409) while tour packages use it.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        (await sender.Send(new DeleteCategoryCommand(id), cancellationToken)).ToActionResult();
}

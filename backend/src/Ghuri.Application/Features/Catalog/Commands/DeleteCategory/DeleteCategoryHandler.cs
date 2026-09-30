using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Catalog.Commands.DeleteCategory;

internal sealed class DeleteCategoryHandler(ICategoryRepository categories, TimeProvider clock)
    : ICommandHandler<DeleteCategoryCommand>
{
    public async ValueTask<Result> Handle(DeleteCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(command.Id, cancellationToken);
        if (category is null)
            return CatalogErrors.CategoryNotFound;

        if (await categories.IsUsedByPackagesAsync(category.Id, cancellationToken))
            return CatalogErrors.CategoryInUse;

        category.MarkDeleted(clock.GetUtcNow().UtcDateTime);
        return Result.Success();
    }
}

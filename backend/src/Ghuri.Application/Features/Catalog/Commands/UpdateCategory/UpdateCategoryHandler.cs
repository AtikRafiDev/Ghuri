using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Catalog.Commands.UpdateCategory;

internal sealed class UpdateCategoryHandler(ICategoryRepository categories) : ICommandHandler<UpdateCategoryCommand>
{
    public async ValueTask<Result> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(command.Id, cancellationToken);
        if (category is null)
            return CatalogErrors.CategoryNotFound;

        if (await categories.NameExistsAsync(command.Name.Trim(), exceptId: category.Id, cancellationToken))
            return CatalogErrors.CategoryNameTaken;

        var slug = CatalogSlug.Build(command.Slug, command.Name);
        if (await categories.SlugExistsAsync(slug, exceptId: category.Id, cancellationToken))
            return CatalogErrors.CategorySlugTaken;

        category.Update(command.Name, slug, command.Icon, command.SortOrder);
        return Result.Success();
    }
}

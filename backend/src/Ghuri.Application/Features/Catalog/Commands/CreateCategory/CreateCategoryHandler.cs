using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Catalog.Commands.CreateCategory;

internal sealed class CreateCategoryHandler(ICategoryRepository categories) : ICommandHandler<CreateCategoryCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        // Two "Beach" categories would confuse customers filtering by type.
        if (await categories.NameExistsAsync(command.Name.Trim(), exceptId: null, cancellationToken))
            return CatalogErrors.CategoryNameTaken;

        var slug = CatalogSlug.Build(command.Slug, command.Name);
        if (await categories.SlugExistsAsync(slug, exceptId: null, cancellationToken))
            return CatalogErrors.CategorySlugTaken;

        var category = Category.Create(command.Name, slug, command.Icon, command.SortOrder);
        categories.Add(category);

        return category.Id;
    }
}

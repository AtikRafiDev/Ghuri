using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Catalog.Queries.GetCategories;

internal sealed class GetCategoriesHandler(IReadDbContext db) : IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async ValueTask<Result<IReadOnlyList<CategoryDto>>> Handle(GetCategoriesQuery query, CancellationToken cancellationToken)
    {
        var rows = await db.Categories
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.Slug, c.Icon })
            .ToListAsync(cancellationToken);

        return rows.Select(c => new CategoryDto(c.Id, c.Name, c.Slug.Value, c.Icon)).ToList();
    }
}

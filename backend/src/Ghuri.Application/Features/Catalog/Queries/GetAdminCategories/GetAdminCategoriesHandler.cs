using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Catalog.Queries.GetAdminCategories;

internal sealed class GetAdminCategoriesHandler(IReadDbContext db)
    : IQueryHandler<GetAdminCategoriesQuery, IReadOnlyList<AdminCategoryDto>>
{
    public async ValueTask<Result<IReadOnlyList<AdminCategoryDto>>> Handle(GetAdminCategoriesQuery query, CancellationToken cancellationToken)
    {
        var rows = await db.Categories
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new
            {
                c.Id, c.Name, c.Slug, c.Icon, c.SortOrder,
                // The link table has no soft delete of its own - so only
                // count links whose package is not deleted.
                PackageCount = db.PackageCategories.Count(pc =>
                    pc.CategoryId == c.Id && db.TourPackages.Any(p => p.Id == pc.PackageId))
            })
            .ToListAsync(cancellationToken);

        return rows.Select(c => new AdminCategoryDto(c.Id, c.Name, c.Slug.Value, c.Icon, c.SortOrder, c.PackageCount)).ToList();
    }
}

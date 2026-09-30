using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Queries.GetAdminCategories;

/// <summary>The admin categories table (blueprint: GET admin/categories). A short list, so not paged.</summary>
public sealed record GetAdminCategoriesQuery : IQuery<IReadOnlyList<AdminCategoryDto>>;

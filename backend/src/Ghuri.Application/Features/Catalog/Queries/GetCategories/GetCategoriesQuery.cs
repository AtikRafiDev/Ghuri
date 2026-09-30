using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Queries.GetCategories;

/// <summary>The public category list (blueprint: GET categories) - search filters, home page chips.</summary>
public sealed record GetCategoriesQuery : IQuery<IReadOnlyList<CategoryDto>>;

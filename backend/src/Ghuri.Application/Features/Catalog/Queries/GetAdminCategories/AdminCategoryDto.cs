namespace Ghuri.Application.Features.Catalog.Queries.GetAdminCategories;

/// <remarks>PackageCount above 0 = delete will be refused.</remarks>
public sealed record AdminCategoryDto(Guid Id, string Name, string Slug, string? Icon, int SortOrder, int PackageCount);

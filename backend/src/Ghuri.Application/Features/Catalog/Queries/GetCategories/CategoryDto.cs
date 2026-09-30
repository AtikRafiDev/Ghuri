namespace Ghuri.Application.Features.Catalog.Queries.GetCategories;

public sealed record CategoryDto(Guid Id, string Name, string Slug, string? Icon);

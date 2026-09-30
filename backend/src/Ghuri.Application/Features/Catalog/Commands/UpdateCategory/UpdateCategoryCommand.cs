using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Commands.UpdateCategory;

/// <summary>Replaces all of a category's fields. Id comes from the URL (PUT /admin/categories/{id}).</summary>
public sealed record UpdateCategoryCommand(Guid Id, string Name, string? Slug, string? Icon, int SortOrder)
    : ICommand, ICategoryFields;

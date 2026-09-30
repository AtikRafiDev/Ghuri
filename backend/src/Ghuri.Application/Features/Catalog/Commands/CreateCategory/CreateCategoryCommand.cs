using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Commands.CreateCategory;

/// <summary>Adds a tour category. Returns its new id.</summary>
public sealed record CreateCategoryCommand(string Name, string? Slug, string? Icon, int SortOrder)
    : ICommand<Guid>, ICategoryFields;

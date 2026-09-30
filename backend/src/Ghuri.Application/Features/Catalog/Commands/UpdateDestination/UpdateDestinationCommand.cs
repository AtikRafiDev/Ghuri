using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Commands.UpdateDestination;

/// <summary>Replaces all of a destination's fields. Id comes from the URL (PUT /admin/destinations/{id}).</summary>
public sealed record UpdateDestinationCommand(
    Guid Id,
    short CountryId,
    string Name,
    string? Slug,
    string? Summary,
    Guid? ImageFileId,
    bool IsFeatured,
    int SortOrder,
    string? SeoTitle,
    string? SeoDescription) : ICommand, IDestinationFields;

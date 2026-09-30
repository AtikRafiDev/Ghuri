using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Commands.CreateDestination;

/// <summary>Adds a destination. Returns its new id.</summary>
public sealed record CreateDestinationCommand(
    short CountryId,
    string Name,
    string? Slug,
    string? Summary,
    Guid? ImageFileId,
    bool IsFeatured,
    int SortOrder,
    string? SeoTitle,
    string? SeoDescription) : ICommand<Guid>, IDestinationFields;

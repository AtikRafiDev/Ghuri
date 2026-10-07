using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Commands.CreateDestination;

/// <summary>Adds a destination. Returns its new id.</summary>
public sealed record CreateDestinationCommand(
    short CountryId,
    string Name,
    string? Slug,
    string? Summary,
    IReadOnlyList<Guid> ImageFileIds,
    bool IsFeatured,
    int SortOrder) : ICommand<Guid>, IDestinationFields;

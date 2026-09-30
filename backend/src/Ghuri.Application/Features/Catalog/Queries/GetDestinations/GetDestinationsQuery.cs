using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Queries.GetDestinations;

/// <summary>
/// The public destination list (blueprint: GET destinations) - home page
/// tiles, search filters, menus. Small (dozens of rows), so not paged.
/// </summary>
/// <param name="Scope">National = Bangladesh only, International = abroad only, null = both.</param>
/// <param name="FeaturedOnly">True = only the ones marked for the home page.</param>
public sealed record GetDestinationsQuery(DestinationScope? Scope, bool FeaturedOnly) : IQuery<IReadOnlyList<DestinationDto>>;

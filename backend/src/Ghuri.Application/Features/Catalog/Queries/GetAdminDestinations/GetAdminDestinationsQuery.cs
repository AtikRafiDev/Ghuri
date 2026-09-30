using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;

namespace Ghuri.Application.Features.Catalog.Queries.GetAdminDestinations;

/// <summary>The admin destinations table: search, filter, page (blueprint: GET admin/destinations).</summary>
/// <remarks>Search matches the destination's or its country's name.</remarks>
public sealed record GetAdminDestinationsQuery(
    string? Search,
    short? CountryId,
    DestinationScope? Scope,
    int Page = 1,
    int PageSize = 20) : IQuery<Paged<AdminDestinationDto>>;

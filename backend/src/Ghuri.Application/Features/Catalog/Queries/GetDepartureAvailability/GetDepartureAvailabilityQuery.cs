using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Queries.GetDepartureAvailability;

/// <summary>
/// The dates a customer can pick on the package page (17-day plan, Day 6:
/// GetDepartureAvailability). Separate from the package details because
/// seats change with every booking - this is fetched fresh, the details can be cached.
/// </summary>
public sealed record GetDepartureAvailabilityQuery(string Slug) : IQuery<IReadOnlyList<AvailableDepartureDto>>;

using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Queries.GetPackageDepartures;

/// <summary>Every departure of one package, earliest first - the admin Departures tab.</summary>
public sealed record GetPackageDeparturesQuery(Guid PackageId) : IQuery<IReadOnlyList<AdminDepartureDto>>;

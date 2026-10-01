using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Queries.GetPackageDetails;

/// <summary>The public package page, by its URL name (/packages/{slug}). Published packages only.</summary>
public sealed record GetPackageDetailsQuery(string Slug) : IQuery<PackageDetailsDto>;

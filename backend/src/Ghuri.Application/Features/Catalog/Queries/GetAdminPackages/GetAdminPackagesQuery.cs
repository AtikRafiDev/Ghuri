using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;

namespace Ghuri.Application.Features.Catalog.Queries.GetAdminPackages;

/// <summary>The admin packages table: search, filter, page (blueprint: GET admin/packages). Newest first.</summary>
/// <remarks>Search matches the title or the package code (PKG1001).</remarks>
public sealed record GetAdminPackagesQuery(
    string? Search,
    Guid? DestinationId,
    PackageStatus? Status,
    PricingMode? PricingMode,
    int Page = 1,
    int PageSize = 20) : IQuery<Paged<AdminPackageListItemDto>>;

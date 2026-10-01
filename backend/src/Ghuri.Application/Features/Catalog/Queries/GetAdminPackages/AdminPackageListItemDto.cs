using Ghuri.Domain.Enums;

namespace Ghuri.Application.Features.Catalog.Queries.GetAdminPackages;

/// <summary>One row of the admin packages table. The edit page loads the full package itself (GetAdminPackage).</summary>
/// <remarks>
/// MinNights/MaxNights are filled only for flexible packages ("2–7 nights").
/// PriceFrom is 0 for a fixed package until it has departures (Day 5).
/// </remarks>
public sealed record AdminPackageListItemDto(
    Guid Id,
    string PackageCode,
    string Title,
    string Slug,
    string DestinationName,
    PricingMode PricingMode,
    PackageStatus Status,
    int DurationDays,
    int DurationNights,
    int? MinNights,
    int? MaxNights,
    decimal PriceFrom,
    string Currency,
    bool IsFeatured,
    string? CoverImageUrl,
    DateTime? PublishedAtUtc);

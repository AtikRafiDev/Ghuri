using Ghuri.Domain.Enums;

namespace Ghuri.Application.Features.Catalog.Queries.SearchPackages;

/// <summary>One package card on the home and search pages.</summary>
/// <remarks>
/// PriceFrom, per adult: a flexible package's base price, or a fixed
/// package's cheapest UPCOMING open departure - worked out live, so a date
/// that has already left never shows its price. Null = a fixed package
/// with no upcoming dates ("New dates coming soon").
/// MinNights/MaxNights are only set for flexible packages ("2–7 nights").
/// </remarks>
public sealed record PackageCardDto(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string DestinationName,
    string? CoverImageUrl,
    PricingMode PricingMode,
    int DurationDays,
    int DurationNights,
    int? MinNights,
    int? MaxNights,
    decimal? PriceFrom,
    string Currency,
    bool IsFeatured);

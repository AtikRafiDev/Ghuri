using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Entities.Catalog;

/// <summary>
/// Every descriptive field of a package, in one object - the admin form
/// always sends them all together. Pricing and duration are separate
/// (PackagePricing) because they have their own rules.
/// </summary>
public sealed record TourPackageDetails(
    Guid DestinationId,
    string Title,
    Slug Slug,
    string Summary,
    string? Description,
    TourType TourType,
    IReadOnlyList<string> Inclusions,
    IReadOnlyList<string> Exclusions,
    string? TermsAndPolicy,
    byte? MinAge,
    bool IsFeatured,
    string? SeoTitle,
    string? SeoDescription);

using Ghuri.Domain.Enums;

namespace Ghuri.Application.Features.Catalog.Queries.GetPackageDetails;

/// <summary>
/// Everything the package page shows except the departure dates, which
/// change with every booking and are fetched separately
/// (GET /packages/{slug}/departures) so they're always fresh.
/// </summary>
/// <remarks>
/// Flexible stays: MinNights..MaxNights, BasePrice (covers MinNights) and
/// ExtraNightPrice are per adult; EarliestStartDate = today + MinLeadDays,
/// for the date picker. SeoTitle/SeoDescription fall back to the title and
/// summary, so the page always has meta tags.
/// </remarks>
public sealed record PackageDetailsDto(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string? Description,
    string DestinationName,
    string DestinationSlug,
    string CountryName,
    TourType TourType,
    IReadOnlyList<PackageCategoryDto> Categories,
    IReadOnlyList<string> Inclusions,
    IReadOnlyList<string> Exclusions,
    string? TermsAndPolicy,
    int? MinAge,
    PricingMode PricingMode,
    int DurationDays,
    int DurationNights,
    int? MinNights,
    int? MaxNights,
    decimal? BasePrice,
    decimal? ExtraNightPrice,
    DateOnly? EarliestStartDate,
    string Currency,
    IReadOnlyList<string> ImageUrls,
    IReadOnlyList<PublicItineraryDayDto> Itinerary,
    string SeoTitle,
    string SeoDescription);

public sealed record PackageCategoryDto(string Name, string Slug, string? Icon);

/// <summary>One itinerary day. Meals are "B,L,D" flags: breakfast, lunch, dinner included.</summary>
public sealed record PublicItineraryDayDto(int DayNo, string Title, string Description, string? Meals, string? Accommodation);

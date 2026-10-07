using Ghuri.Domain.Enums;

namespace Ghuri.Application.Features.Catalog.Queries.GetAdminPackage;

/// <summary>
/// The whole package. The form fields have the same names as in
/// CreatePackageCommand/UpdatePackageCommand, so the edit form can load
/// this and send it back almost unchanged.
/// </summary>
/// <remarks>
/// PublishProblems: what still stops it going live (empty = ready). The
/// publish button can list these up front instead of waiting for a failed click.
/// </remarks>
public sealed record AdminPackageDto(
    Guid Id,
    string PackageCode,
    PackageStatus Status,
    Guid DestinationId,
    string Title,
    string Slug,
    string Summary,
    string? Description,
    TourType TourType,
    IReadOnlyList<Guid> CategoryIds,
    IReadOnlyList<string> Inclusions,
    IReadOnlyList<string> Exclusions,
    string? TermsAndPolicy,
    int? MinAge,
    bool IsFeatured,
    PricingMode PricingMode,
    int DurationDays,
    int DurationNights,
    int? MinNights,
    int? MaxNights,
    decimal? BasePrice,
    decimal? ExtraNightPrice,
    int? MinLeadDays,
    decimal PriceFrom,
    string Currency,
    DateTime? PublishedAtUtc,
    IReadOnlyList<PackageImageDto> Images,
    IReadOnlyList<ItineraryDayDto> ItineraryDays,
    IReadOnlyList<string> PublishProblems);

/// <remarks>In display order - the first is the cover. FileId is what the form sends back; Url is for showing it.</remarks>
public sealed record PackageImageDto(Guid FileId, string Url);

public sealed record ItineraryDayDto(int DayNo, string Title, string Description, string? Meals, string? Accommodation);

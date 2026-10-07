using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Domain.Enums;

namespace Ghuri.Application.Features.Catalog.Commands.UpdatePackage;

/// <summary>Replaces all of a package's form fields. Id comes from the URL (PUT /admin/packages/{id}).</summary>
public sealed record UpdatePackageCommand(
    Guid Id,
    Guid DestinationId,
    string Title,
    string? Slug,
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
    int? DurationDays,
    int? DurationNights,
    int? MinNights,
    int? MaxNights,
    decimal? BasePrice,
    decimal? ExtraNightPrice,
    int? MinLeadDays) : ICommand, IPackageFields;

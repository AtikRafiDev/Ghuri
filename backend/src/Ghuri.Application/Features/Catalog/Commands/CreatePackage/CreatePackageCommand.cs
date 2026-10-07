using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Domain.Enums;

namespace Ghuri.Application.Features.Catalog.Commands.CreatePackage;

/// <summary>Adds a package as a Draft. Returns its new id. See IPackageFields for which pricing fields each mode needs.</summary>
public sealed record CreatePackageCommand(
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
    int? MinLeadDays) : ICommand<Guid>, IPackageFields;

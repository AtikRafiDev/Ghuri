using FluentValidation;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Application.Features.Catalog;

/// <summary>
/// The fields of the package form, shared by Create and Update so their
/// rules can't drift apart. Photos and the itinerary are NOT here - they
/// have their own tabs and commands (SetPackageImages, SavePackageItinerary).
/// </summary>
/// <remarks>
/// Pricing is flat on purpose (simple JSON for the form): which fields are
/// needed depends on PricingMode. Fixed departures → DurationDays and
/// DurationNights. Flexible stay → MinNights, MaxNights, BasePrice,
/// ExtraNightPrice and MinLeadDays; its duration is worked out from MinNights.
/// The fields the other mode needs are ignored.
/// </remarks>
public interface IPackageFields
{
    Guid DestinationId { get; }
    string Title { get; }

    /// <summary>Optional - left empty, it is made from Title.</summary>
    string? Slug { get; }

    string Summary { get; }
    string? Description { get; }
    TourType TourType { get; }
    IReadOnlyList<Guid> CategoryIds { get; }
    IReadOnlyList<string> Inclusions { get; }
    IReadOnlyList<string> Exclusions { get; }
    string? TermsAndPolicy { get; }
    int? MinAge { get; }
    bool IsFeatured { get; }
    string? SeoTitle { get; }
    string? SeoDescription { get; }

    PricingMode PricingMode { get; }
    int? DurationDays { get; }
    int? DurationNights { get; }
    int? MinNights { get; }
    int? MaxNights { get; }

    /// <summary>Per adult, covers MinNights.</summary>
    decimal? BasePrice { get; }

    /// <summary>Per adult, for each night after MinNights.</summary>
    decimal? ExtraNightPrice { get; }

    int? MinLeadDays { get; }
}

/// <summary>
/// Lengths match the catalog.TourPackages columns. The number limits are
/// the same as PackagePricing's, so a form that passes here never makes
/// the domain throw.
/// </summary>
internal sealed class PackageFieldsValidator : AbstractValidator<IPackageFields>
{
    private const int MaxListItems = 30;
    private const int LongTextLength = 20_000;

    public PackageFieldsValidator()
    {
        RuleFor(x => x.DestinationId).NotEmpty().WithMessage("Choose a destination.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).ValidSlugFor(x => x.Title, maxLength: 220);
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Description).MaximumLength(LongTextLength);
        RuleFor(x => x.TourType).IsInEnum();

        // Missing lists (null in the JSON) are treated as empty by the handlers.
        RuleFor(x => x.CategoryIds)
            .Must(ids => ids is null || ids.Count <= 10).WithMessage("At most 10 categories.")
            .Must(ids => ids is null || !ids.Contains(Guid.Empty)).WithMessage("A category id is missing.");

        RuleFor(x => x.Inclusions)
            .Must(items => items is null || items.Count <= MaxListItems).WithMessage($"At most {MaxListItems} points.");
        RuleForEach(x => x.Inclusions).MaximumLength(200);
        RuleFor(x => x.Exclusions)
            .Must(items => items is null || items.Count <= MaxListItems).WithMessage($"At most {MaxListItems} points.");
        RuleForEach(x => x.Exclusions).MaximumLength(200);

        RuleFor(x => x.TermsAndPolicy).MaximumLength(LongTextLength);
        RuleFor(x => x.MinAge).InclusiveBetween(0, 100);
        RuleFor(x => x.SeoTitle).MaximumLength(70);
        RuleFor(x => x.SeoDescription).MaximumLength(160);

        RuleFor(x => x.PricingMode).IsInEnum().WithMessage("Choose Fixed departures or Flexible stay.");

        When(x => x.PricingMode == PricingMode.FixedDepartures, () =>
        {
            RuleFor(x => x.DurationDays).NotNull().WithMessage("Enter the number of days.")
                .InclusiveBetween(1, PackagePricing.MaxDays);
            RuleFor(x => x.DurationNights).NotNull().WithMessage("Enter the number of nights.")
                .InclusiveBetween(0, PackagePricing.MaxDays);
        });

        When(x => x.PricingMode == PricingMode.FlexibleStay, () =>
        {
            // The longest stay (MaxNights + 1 days) must fit the 60-day limit.
            const int maxNights = PackagePricing.MaxDays - 1;

            RuleFor(x => x.MinNights).NotNull().WithMessage("Enter the minimum nights.")
                .InclusiveBetween(1, maxNights);
            RuleFor(x => x.MaxNights).NotNull().WithMessage("Enter the maximum nights.")
                .InclusiveBetween(1, maxNights)
                .Must((x, max) => x.MinNights is null || max >= x.MinNights)
                .WithMessage("Maximum nights cannot be less than minimum nights.");
            RuleFor(x => x.BasePrice).NotNull().WithMessage("Enter the base price.")
                .GreaterThan(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
            RuleFor(x => x.ExtraNightPrice).NotNull().WithMessage("Enter the extra-night price (0 if free).")
                .GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
            RuleFor(x => x.MinLeadDays).NotNull().WithMessage("Enter how many days ahead it must be booked (0 = same day).")
                .InclusiveBetween(0, 90);
        });
    }
}

/// <summary>Turns the (already validated) form fields into the domain's input objects.</summary>
internal static class PackageFieldsMapping
{
    public static TourPackageDetails ToDetails(this IPackageFields fields, Slug slug) => new(
        fields.DestinationId,
        fields.Title,
        slug,
        fields.Summary,
        fields.Description,
        fields.TourType,
        fields.Inclusions ?? [],
        fields.Exclusions ?? [],
        fields.TermsAndPolicy,
        (byte?)fields.MinAge,
        fields.IsFeatured,
        fields.SeoTitle,
        fields.SeoDescription);

    /// <summary>The validator has already made sure the needed fields are filled, so the "!" can't fail.</summary>
    public static PackagePricing ToPricing(this IPackageFields fields) =>
        fields.PricingMode == PricingMode.FlexibleStay
            ? PackagePricing.FlexibleStay(
                (byte)fields.MinNights!.Value, (byte)fields.MaxNights!.Value,
                fields.BasePrice!.Value, fields.ExtraNightPrice!.Value, (byte)fields.MinLeadDays!.Value)
            : PackagePricing.FixedDepartures((byte)fields.DurationDays!.Value, (byte)fields.DurationNights!.Value);
}

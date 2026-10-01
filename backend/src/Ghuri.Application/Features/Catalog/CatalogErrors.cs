using Ghuri.Application.Common;

namespace Ghuri.Application.Features.Catalog;

/// <summary>Every expected failure of the Catalog feature. Codes never change once shipped.</summary>
public static class CatalogErrors
{
    // Shared input checks - 400, the form has to be corrected.
    public static readonly Error CountryNotFound =
        Error.Failure("country_not_found", "Choose a country from the list.");

    public static readonly Error ImageNotFound =
        Error.Failure("image_not_found", "A photo was not found. Please upload it again.");

    // Destinations
    public static readonly Error DestinationNotFound =
        Error.NotFound("destination_not_found", "This destination does not exist or was deleted.");

    public static readonly Error DestinationSlugTaken =
        Error.Conflict("destination_slug_taken", "Another destination already uses this URL name.");

    public static readonly Error DestinationInUse =
        Error.Conflict("destination_in_use", "Tour packages still use this destination. Move or delete them first.");

    // Categories
    public static readonly Error CategoryNotFound =
        Error.NotFound("category_not_found", "This category does not exist or was deleted.");

    public static readonly Error CategoryNameTaken =
        Error.Conflict("category_name_taken", "Another category already has this name.");

    public static readonly Error CategorySlugTaken =
        Error.Conflict("category_slug_taken", "Another category already uses this URL name.");

    public static readonly Error CategoryInUse =
        Error.Conflict("category_in_use", "Tour packages still use this category. Remove it from them first.");

    // Tour packages - the package form points at destinations and categories,
    // so an unknown one is a form mistake (400), not a missing page (404).
    public static readonly Error PackageDestinationNotFound =
        Error.Failure("destination_not_found", "Choose a destination from the list.");

    public static readonly Error PackageCategoryNotFound =
        Error.Failure("category_not_found", "A category was not found. Refresh the page and choose again.");

    public static readonly Error PackageNotFound =
        Error.NotFound("package_not_found", "This package does not exist or was deleted.");

    public static readonly Error PackageSlugTaken =
        Error.Conflict("package_slug_taken", "Another package already uses this URL name.");

    public static readonly Error PackagePricingModeLocked =
        Error.Conflict("package_pricing_mode_locked", "The pricing mode can only be changed while the package is a draft.");

    public static readonly Error PackageAlreadyPublished =
        Error.Conflict("package_already_published", "This package is already published.");

    public static readonly Error PackageAlreadyArchived =
        Error.Conflict("package_already_archived", "This package is already archived.");

    /// <summary>The message lists every problem - see TourPackage.GetPublishProblems.</summary>
    public static Error PackageNotPublishable(IReadOnlyList<string> problems) =>
        Error.Conflict("package_not_publishable", string.Join(" ", problems));
}

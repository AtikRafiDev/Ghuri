using Ghuri.Application.Common;

namespace Ghuri.Application.Features.Catalog;

/// <summary>Every expected failure of the Catalog feature. Codes never change once shipped.</summary>
public static class CatalogErrors
{
    // Shared input checks - 400, the form has to be corrected.
    public static readonly Error CountryNotFound =
        Error.Failure("country_not_found", "Choose a country from the list.");

    public static readonly Error ImageNotFound =
        Error.Failure("image_not_found", "The image was not found. Please upload it again.");

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
}

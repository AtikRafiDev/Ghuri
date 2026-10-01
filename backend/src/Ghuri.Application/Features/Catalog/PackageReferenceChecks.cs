using Ghuri.Application.Common;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Catalog;

/// <summary>The checks Create and Update both run before touching a package, in one place.</summary>
internal static class PackageReferenceChecks
{
    /// <summary>
    /// Does the chosen destination exist, and every chosen category? Checked
    /// here so the admin gets a clear message instead of a database
    /// foreign-key error (a 500). Returns null when everything is fine.
    /// </summary>
    public static async Task<Error?> CheckAsync(
        IPackageFields fields,
        IDestinationRepository destinations,
        ICategoryRepository categories,
        CancellationToken cancellationToken)
    {
        if (!await destinations.ExistsAsync(fields.DestinationId, cancellationToken))
            return CatalogErrors.PackageDestinationNotFound;

        if (!await categories.AllExistAsync(fields.CategoryIds ?? [], cancellationToken))
            return CatalogErrors.PackageCategoryNotFound;

        return null;
    }
}

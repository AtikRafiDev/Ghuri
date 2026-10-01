using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Application.Features.Catalog.Commands.UpdatePackage;

internal sealed class UpdatePackageHandler(
    ITourPackageRepository packages,
    IDestinationRepository destinations,
    ICategoryRepository categories,
    IDepartureRepository departures)
    : ICommandHandler<UpdatePackageCommand>
{
    public async ValueTask<Result> Handle(UpdatePackageCommand command, CancellationToken cancellationToken)
    {
        var package = await packages.GetByIdAsync(command.Id, cancellationToken);
        if (package is null)
            return CatalogErrors.PackageNotFound;

        if (await PackageReferenceChecks.CheckAsync(command, destinations, categories, cancellationToken) is { } error)
            return error;

        // The domain would refuse this too (DomainException → 422); checking
        // first gives the form a normal 409 with a stable code instead.
        if (package.Status != PackageStatus.Draft && command.PricingMode != package.PricingMode)
            return CatalogErrors.PackagePricingModeLocked;

        var slug = CatalogSlug.Build(command.Slug, command.Title);
        if (await packages.SlugExistsAsync(slug, exceptId: package.Id, cancellationToken))
            return CatalogErrors.PackageSlugTaken;

        var pricing = command.ToPricing();
        if (await KeepDeparturesInStepAsync(package, pricing, cancellationToken) is { } departureError)
            return departureError;

        // No "save" call: the package was loaded with tracking, so EF Core
        // sees these changes when TransactionBehavior commits.
        package.Update(command.ToDetails(slug));
        package.ChangePricing(pricing);
        package.SetCategories(command.CategoryIds ?? []);

        return Result.Success();
    }

    /// <summary>
    /// A fixed package's departures take their EndDate from its duration.
    /// Switching to flexible would leave them pointless, so that's refused;
    /// a new duration moves their end dates along - unless seats are
    /// booked, because those customers bought the old dates.
    /// </summary>
    private async Task<Error?> KeepDeparturesInStepAsync(
        TourPackage package, PackagePricing pricing, CancellationToken cancellationToken)
    {
        if (package.PricingMode != PricingMode.FixedDepartures)
            return null; // a flexible package never has departures

        var modeChanges = pricing.Mode != PricingMode.FixedDepartures;
        if (!modeChanges && pricing.DurationDays == package.DurationDays)
            return null;

        var existing = await departures.ListForPackageAsync(package.Id, cancellationToken);
        if (existing.Count == 0)
            return null;

        if (modeChanges)
            return CatalogErrors.PackageHasDepartures;
        if (existing.Any(d => d.ReservedSeats > 0))
            return CatalogErrors.PackageDurationLocked;

        foreach (var departure in existing)
            departure.ChangeDuration(pricing.DurationDays);
        return null;
    }
}

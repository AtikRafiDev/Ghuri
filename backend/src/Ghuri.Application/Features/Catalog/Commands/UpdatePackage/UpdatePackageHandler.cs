using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Catalog.Commands.UpdatePackage;

internal sealed class UpdatePackageHandler(
    ITourPackageRepository packages,
    IDestinationRepository destinations,
    ICategoryRepository categories)
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

        // No "save" call: the package was loaded with tracking, so EF Core
        // sees these changes when TransactionBehavior commits.
        package.Update(command.ToDetails(slug));
        package.ChangePricing(command.ToPricing());
        package.SetCategories(command.CategoryIds ?? []);

        return Result.Success();
    }
}

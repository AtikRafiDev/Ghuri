using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Catalog.Commands.CreatePackage;

internal sealed class CreatePackageHandler(
    ITourPackageRepository packages,
    IDestinationRepository destinations,
    ICategoryRepository categories)
    : ICommandHandler<CreatePackageCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CreatePackageCommand command, CancellationToken cancellationToken)
    {
        if (await PackageReferenceChecks.CheckAsync(command, destinations, categories, cancellationToken) is { } error)
            return error;

        var slug = CatalogSlug.Build(command.Slug, command.Title);
        if (await packages.SlugExistsAsync(slug, exceptId: null, cancellationToken))
            return CatalogErrors.PackageSlugTaken;

        // Taken last, after every check: a sequence number is used up even
        // if the transaction rolls back, so failing earlier wastes none.
        var packageCode = await packages.NextPackageCodeAsync(cancellationToken);

        var package = TourPackage.Create(packageCode, command.ToDetails(slug), command.ToPricing());
        package.SetCategories(command.CategoryIds ?? []);
        packages.Add(package);

        return package.Id;
    }
}

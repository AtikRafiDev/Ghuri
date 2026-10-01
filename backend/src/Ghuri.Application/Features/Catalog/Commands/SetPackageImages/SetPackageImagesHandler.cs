using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Catalog.Commands.SetPackageImages;

internal sealed class SetPackageImagesHandler(ITourPackageRepository packages, IFileObjectRepository files)
    : ICommandHandler<SetPackageImagesCommand>
{
    public async ValueTask<Result> Handle(SetPackageImagesCommand command, CancellationToken cancellationToken)
    {
        // Loaded WITH its photos - SetImages compares the new list to them.
        var package = await packages.GetByIdAsync(command.Id, cancellationToken);
        if (package is null)
            return CatalogErrors.PackageNotFound;

        var imageFileIds = command.ImageFileIds ?? [];
        if (!await files.AllExistAsync(imageFileIds, cancellationToken))
            return CatalogErrors.ImageNotFound;

        // On a PUBLISHED package, removing every photo throws a
        // DomainException (422 package_must_stay_publishable) - see TourPackage.
        package.SetImages(imageFileIds);
        return Result.Success();
    }
}

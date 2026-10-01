using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Catalog.Commands.ArchivePackage;

internal sealed class ArchivePackageHandler(ITourPackageRepository packages)
    : ICommandHandler<ArchivePackageCommand>
{
    public async ValueTask<Result> Handle(ArchivePackageCommand command, CancellationToken cancellationToken)
    {
        var package = await packages.GetByIdAsync(command.Id, cancellationToken);
        if (package is null)
            return CatalogErrors.PackageNotFound;

        if (package.Status == PackageStatus.Archived)
            return CatalogErrors.PackageAlreadyArchived;

        package.Archive();
        return Result.Success();
    }
}

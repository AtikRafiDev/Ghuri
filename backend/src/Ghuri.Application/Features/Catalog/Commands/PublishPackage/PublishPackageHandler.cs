using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Catalog.Commands.PublishPackage;

internal sealed class PublishPackageHandler(ITourPackageRepository packages, TimeProvider clock)
    : ICommandHandler<PublishPackageCommand>
{
    public async ValueTask<Result> Handle(PublishPackageCommand command, CancellationToken cancellationToken)
    {
        var package = await packages.GetByIdAsync(command.Id, cancellationToken);
        if (package is null)
            return CatalogErrors.PackageNotFound;

        if (package.Status == PackageStatus.Published)
            return CatalogErrors.PackageAlreadyPublished;

        // Ask first, so every problem comes back as a normal 409 the admin
        // can read - Publish itself would throw on the same rules.
        var problems = package.GetPublishProblems();
        if (problems.Count > 0)
            return CatalogErrors.PackageNotPublishable(problems);

        package.Publish(clock.GetUtcNow().UtcDateTime);
        return Result.Success();
    }
}

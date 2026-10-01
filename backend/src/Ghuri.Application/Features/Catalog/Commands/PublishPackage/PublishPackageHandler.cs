using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Catalog.Commands.PublishPackage;

internal sealed class PublishPackageHandler(
    ITourPackageRepository packages,
    IDepartureRepository departures,
    TimeProvider clock)
    : ICommandHandler<PublishPackageCommand>
{
    public async ValueTask<Result> Handle(PublishPackageCommand command, CancellationToken cancellationToken)
    {
        var package = await packages.GetByIdAsync(command.Id, cancellationToken);
        if (package is null)
            return CatalogErrors.PackageNotFound;

        if (package.Status == PackageStatus.Published)
            return CatalogErrors.PackageAlreadyPublished;

        // Departures are their own aggregate: look it up here, let the
        // package's own rule decide (TourPackage.GetPublishProblems).
        var hasOpenDeparture = await departures.HasOpenUpcomingAsync(package.Id, clock.Today(), cancellationToken);

        // Ask first, so every problem comes back as a normal 409 the admin
        // can read - Publish itself would throw on the same rules.
        var problems = package.GetPublishProblems(hasOpenDeparture);
        if (problems.Count > 0)
            return CatalogErrors.PackageNotPublishable(problems);

        package.Publish(clock.GetUtcNow().UtcDateTime, hasOpenDeparture);
        return Result.Success();
    }
}

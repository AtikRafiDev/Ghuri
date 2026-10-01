using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Catalog.Commands.CloseDeparture;

internal sealed class CloseDepartureHandler(
    ITourPackageRepository packages,
    IDepartureRepository departures,
    TimeProvider clock)
    : ICommandHandler<CloseDepartureCommand>
{
    public async ValueTask<Result> Handle(CloseDepartureCommand command, CancellationToken cancellationToken)
    {
        var departure = await departures.GetByIdAsync(command.Id, cancellationToken);
        if (departure is null)
            return CatalogErrors.DepartureNotFound;

        if (departure.Status != DepartureStatus.Open)
            return CatalogErrors.DepartureNotOpen;

        var package = await packages.GetByIdAsync(departure.PackageId, cancellationToken);
        if (package is null)
            return CatalogErrors.PackageNotFound;

        departure.Close();

        // Closing the cheapest date raises the package's "from ৳…" price -
        // or sets it to 0 when no open date is left. The package itself
        // stays published: no upcoming dates is a normal "sold out".
        await PackagePriceFrom.RefreshAsync(package, departure, departures, clock.Today(), cancellationToken);
        return Result.Success();
    }
}

using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Catalog.Commands.CreateDeparture;

internal sealed class CreateDepartureHandler(
    ITourPackageRepository packages,
    IDepartureRepository departures,
    TimeProvider clock)
    : ICommandHandler<CreateDepartureCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CreateDepartureCommand command, CancellationToken cancellationToken)
    {
        var package = await packages.GetByIdAsync(command.PackageId, cancellationToken);
        if (package is null)
            return CatalogErrors.PackageNotFound;

        // A flexible stay has no dates - the customer picks them.
        if (package.PricingMode != PricingMode.FixedDepartures)
            return CatalogErrors.DepartureNeedsFixedPackage;

        var today = clock.Today();
        if (command.StartDate < today)
            return CatalogErrors.DepartureDateInPast;

        if (await departures.ExistsOnDateAsync(package.Id, command.StartDate, exceptId: null, cancellationToken))
            return CatalogErrors.DepartureDateTaken;

        var departure = Departure.Create(
            package.Id, command.StartDate, package.DurationDays,
            command.AdultPrice, command.ChildPrice, command.InfantPrice, command.SingleSupplement,
            (short)command.TotalSeats, (byte)command.BookingCutoffDays);
        departures.Add(departure);

        // A cheaper new date lowers the package's "from ৳…" price.
        await PackagePriceFrom.RefreshAsync(package, departure, departures, today, cancellationToken);

        return departure.Id;
    }
}

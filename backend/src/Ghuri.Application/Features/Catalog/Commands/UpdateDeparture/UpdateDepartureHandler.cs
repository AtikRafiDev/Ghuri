using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Catalog.Commands.UpdateDeparture;

internal sealed class UpdateDepartureHandler(
    ITourPackageRepository packages,
    IDepartureRepository departures,
    TimeProvider clock)
    : ICommandHandler<UpdateDepartureCommand>
{
    public async ValueTask<Result> Handle(UpdateDepartureCommand command, CancellationToken cancellationToken)
    {
        var departure = await departures.GetByIdAsync(command.Id, cancellationToken);
        if (departure is null)
            return CatalogErrors.DepartureNotFound;

        var package = await packages.GetByIdAsync(departure.PackageId, cancellationToken);
        if (package is null)
            return CatalogErrors.PackageNotFound;

        // The domain refuses these too (DomainException → 422); checked here
        // first so the form gets a normal 409 with a stable code.
        if (departure.Status is DepartureStatus.Cancelled or DepartureStatus.Completed)
            return CatalogErrors.DepartureNotEditable;
        if (command.TotalSeats < departure.ReservedSeats)
            return CatalogErrors.DepartureSeatsBelowReserved(departure.ReservedSeats);

        var today = clock.Today();
        if (command.StartDate != departure.StartDate)
        {
            if (departure.ReservedSeats > 0)
                return CatalogErrors.DepartureDatesLocked;
            if (command.StartDate < today)
                return CatalogErrors.DepartureDateInPast;
            if (await departures.ExistsOnDateAsync(package.Id, command.StartDate, exceptId: departure.Id, cancellationToken))
                return CatalogErrors.DepartureDateTaken;
        }

        departure.Update(
            command.StartDate, package.DurationDays,
            command.AdultPrice, command.ChildPrice, command.InfantPrice, command.SingleSupplement,
            (short)command.TotalSeats, (byte)command.BookingCutoffDays);

        await PackagePriceFrom.RefreshAsync(package, departure, departures, today, cancellationToken);
        return Result.Success();
    }
}

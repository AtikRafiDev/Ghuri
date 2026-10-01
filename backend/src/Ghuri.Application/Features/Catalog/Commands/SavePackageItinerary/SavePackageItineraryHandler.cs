using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Catalog.Commands.SavePackageItinerary;

internal sealed class SavePackageItineraryHandler(ITourPackageRepository packages)
    : ICommandHandler<SavePackageItineraryCommand>
{
    public async ValueTask<Result> Handle(SavePackageItineraryCommand command, CancellationToken cancellationToken)
    {
        var package = await packages.GetByIdAsync(command.Id, cancellationToken);
        if (package is null)
            return CatalogErrors.PackageNotFound;

        var days = (command.Days ?? [])
            .Select(d => new ItineraryDayDetails(d.Title, d.Description, d.Meals, d.Accommodation))
            .ToList();

        // On a PUBLISHED fixed package the day count must still match the
        // duration, or this throws a DomainException (422) - see TourPackage.
        package.SetItinerary(days);
        return Result.Success();
    }
}

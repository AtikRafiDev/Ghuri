using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Catalog.Commands.DeleteDestination;

internal sealed class DeleteDestinationHandler(IDestinationRepository destinations, TimeProvider clock)
    : ICommandHandler<DeleteDestinationCommand>
{
    public async ValueTask<Result> Handle(DeleteDestinationCommand command, CancellationToken cancellationToken)
    {
        var destination = await destinations.GetByIdAsync(command.Id, cancellationToken);
        if (destination is null)
            return CatalogErrors.DestinationNotFound;

        // A package without a destination would break its public page.
        if (await destinations.IsUsedByPackagesAsync(destination.Id, cancellationToken))
            return CatalogErrors.DestinationInUse;

        destination.MarkDeleted(clock.GetUtcNow().UtcDateTime);
        return Result.Success();
    }
}

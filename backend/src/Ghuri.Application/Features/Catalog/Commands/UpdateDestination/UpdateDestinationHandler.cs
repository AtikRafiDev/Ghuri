using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Catalog.Commands.UpdateDestination;

internal sealed class UpdateDestinationHandler(IDestinationRepository destinations, IFileObjectRepository files)
    : ICommandHandler<UpdateDestinationCommand>
{
    public async ValueTask<Result> Handle(UpdateDestinationCommand command, CancellationToken cancellationToken)
    {
        // Loaded WITH its photos - SetImages compares the new list to them.
        var destination = await destinations.GetByIdAsync(command.Id, cancellationToken);
        if (destination is null)
            return CatalogErrors.DestinationNotFound;

        if (!await destinations.CountryExistsAsync(command.CountryId, cancellationToken))
            return CatalogErrors.CountryNotFound;

        var imageFileIds = command.ImageFileIds ?? [];
        if (!await files.AllExistAsync(imageFileIds, cancellationToken))
            return CatalogErrors.ImageNotFound;

        var slug = CatalogSlug.Build(command.Slug, command.Name);
        if (await destinations.SlugExistsAsync(slug, exceptId: destination.Id, cancellationToken))
            return CatalogErrors.DestinationSlugTaken;

        // No "save" call: the destination was loaded with tracking, so EF
        // Core sees these changes when TransactionBehavior commits.
        destination.Update(
            command.CountryId, command.Name, slug, command.Summary,
            command.IsFeatured, command.SortOrder, command.SeoTitle, command.SeoDescription);
        destination.SetImages(imageFileIds);

        return Result.Success();
    }
}

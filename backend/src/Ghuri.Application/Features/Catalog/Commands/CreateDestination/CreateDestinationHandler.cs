using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Catalog.Commands.CreateDestination;

internal sealed class CreateDestinationHandler(IDestinationRepository destinations, IFileObjectRepository files)
    : ICommandHandler<CreateDestinationCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CreateDestinationCommand command, CancellationToken cancellationToken)
    {
        // Checked here so the admin gets a clear message instead of a
        // database foreign-key / unique-index error (a 500).
        if (!await destinations.CountryExistsAsync(command.CountryId, cancellationToken))
            return CatalogErrors.CountryNotFound;

        var imageFileIds = command.ImageFileIds ?? []; // "imageFileIds" left out of the JSON = no photos
        if (!await files.AllExistAsync(imageFileIds, cancellationToken))
            return CatalogErrors.ImageNotFound;

        var slug = CatalogSlug.Build(command.Slug, command.Name);
        if (await destinations.SlugExistsAsync(slug, exceptId: null, cancellationToken))
            return CatalogErrors.DestinationSlugTaken;

        var destination = Destination.Create(
            command.CountryId, command.Name, slug, command.Summary,
            command.IsFeatured, command.SortOrder);
        destination.SetImages(imageFileIds);
        destinations.Add(destination);

        return destination.Id;
    }
}

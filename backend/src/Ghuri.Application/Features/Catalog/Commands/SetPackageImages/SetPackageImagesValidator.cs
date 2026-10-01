using FluentValidation;
using Ghuri.Domain.Entities.Catalog;

namespace Ghuri.Application.Features.Catalog.Commands.SetPackageImages;

internal sealed class SetPackageImagesValidator : AbstractValidator<SetPackageImagesCommand>
{
    public SetPackageImagesValidator()
    {
        // A missing list (null in the JSON) is treated as "no photos" by the handler.
        RuleFor(x => x.ImageFileIds)
            .Must(ids => ids is null || ids.Count <= TourPackage.MaxImages)
            .WithMessage($"At most {TourPackage.MaxImages} photos.")
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithMessage("The same photo is listed twice.")
            .Must(ids => ids is null || !ids.Contains(Guid.Empty))
            .WithMessage("A photo id is missing.");
    }
}

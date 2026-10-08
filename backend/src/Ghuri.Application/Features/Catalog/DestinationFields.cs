using FluentValidation;
using Ghuri.Domain.Entities.Catalog;

namespace Ghuri.Application.Features.Catalog;

/// <summary>The fields of the destination form, shared by Create and Update so their rules can't drift apart.</summary>
public interface IDestinationFields
{
    short CountryId { get; }
    string Name { get; }

    /// <summary>Optional - left empty, it is made from Name.</summary>
    string? Slug { get; }

    string? Summary { get; }

    /// <summary>The gallery: ids of already-uploaded files (POST /api/v1/files), in display order - the first is the cover.</summary>
    IReadOnlyList<Guid> ImageFileIds { get; }

    bool IsFeatured { get; }

    /// <summary>Lower comes first. Null = after the last destination. A number already in use moves that destination (and any right behind it) along by one.</summary>
    int? SortOrder { get; }
}

/// <summary>Lengths match the catalog.Destinations columns, so the database never has to truncate or refuse.</summary>
internal sealed class DestinationFieldsValidator : AbstractValidator<IDestinationFields>
{
    public DestinationFieldsValidator()
    {
        RuleFor(x => x.CountryId).GreaterThan((short)0).WithMessage("Choose a country.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Slug).ValidSlugFor(x => x.Name, maxLength: 160);
        RuleFor(x => x.Summary).MaximumLength(500);

        // A missing list (null in the JSON) is treated as "no photos" by the handlers.
        RuleFor(x => x.ImageFileIds)
            .Must(ids => ids is null || ids.Count <= Destination.MaxImages)
            .WithMessage($"At most {Destination.MaxImages} photos.")
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithMessage("The same photo is listed twice.")
            .Must(ids => ids is null || !ids.Contains(Guid.Empty))
            .WithMessage("A photo id is missing.");

        RuleFor(x => x.SortOrder).InclusiveBetween(0, 100_000); // null passes: "put it at the end"
    }
}

using FluentValidation;

namespace Ghuri.Application.Features.Catalog;

/// <summary>The fields of the destination form, shared by Create and Update so their rules can't drift apart.</summary>
public interface IDestinationFields
{
    short CountryId { get; }
    string Name { get; }

    /// <summary>Optional - left empty, it is made from Name.</summary>
    string? Slug { get; }

    string? Summary { get; }
    Guid? ImageFileId { get; }
    bool IsFeatured { get; }
    int SortOrder { get; }
    string? SeoTitle { get; }
    string? SeoDescription { get; }
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
        RuleFor(x => x.SortOrder).InclusiveBetween(0, 100_000);
        // Google shows about this much of a title / description in results.
        RuleFor(x => x.SeoTitle).MaximumLength(70);
        RuleFor(x => x.SeoDescription).MaximumLength(160);
    }
}

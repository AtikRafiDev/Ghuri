using FluentValidation;

namespace Ghuri.Application.Features.Catalog;

/// <summary>The fields of the category form, shared by Create and Update.</summary>
public interface ICategoryFields
{
    string Name { get; }

    /// <summary>Optional - left empty, it is made from Name.</summary>
    string? Slug { get; }

    /// <summary>A lucide-react icon name, e.g. "umbrella".</summary>
    string? Icon { get; }

    int SortOrder { get; }
}

internal sealed class CategoryFieldsValidator : AbstractValidator<ICategoryFields>
{
    public CategoryFieldsValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Slug).ValidSlugFor(x => x.Name, maxLength: 120);
        RuleFor(x => x.Icon)
            .MaximumLength(50)
            .Matches("^[a-z0-9-]+$").WithMessage("Use an icon name like \"umbrella\" or \"mountain\".")
            .When(x => !string.IsNullOrWhiteSpace(x.Icon));
        RuleFor(x => x.SortOrder).InclusiveBetween(0, 100_000);
    }
}

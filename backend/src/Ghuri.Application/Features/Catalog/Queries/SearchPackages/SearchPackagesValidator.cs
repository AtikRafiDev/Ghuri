using FluentValidation;
using Ghuri.Application.Common;

namespace Ghuri.Application.Features.Catalog.Queries.SearchPackages;

internal sealed class SearchPackagesValidator : AbstractValidator<SearchPackagesQuery>
{
    public SearchPackagesValidator()
    {
        RuleFor(x => x.Q).MaximumLength(100);
        RuleFor(x => x.Destination).MaximumLength(220);
        RuleFor(x => x.Category).MaximumLength(220);
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0)
            .GreaterThanOrEqualTo(x => x.MinPrice).When(x => x.MinPrice is not null)
            .WithMessage("The highest price can't be below the lowest.");
        RuleFor(x => x.Mode).IsInEnum();
        RuleFor(x => x.Sort).IsInEnum();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, Paged<PackageCardDto>.MaxPageSize);
    }
}

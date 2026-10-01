using FluentValidation;
using Ghuri.Application.Common;

namespace Ghuri.Application.Features.Catalog.Queries.GetAdminPackages;

internal sealed class GetAdminPackagesValidator : AbstractValidator<GetAdminPackagesQuery>
{
    public GetAdminPackagesValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.PricingMode).IsInEnum();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, Paged<AdminPackageListItemDto>.MaxPageSize);
    }
}

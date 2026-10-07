using Ghuri.Application.Features.Catalog.Commands.CreatePackage;
using Ghuri.Domain.Enums;

namespace Ghuri.Application.Tests.Features.Catalog;

public class PackageFieldsValidatorTests
{
    private static readonly CreatePackageCommand Fixed = new(
        Guid.NewGuid(), "Cox's Bazar 3 Days", Slug: null, Summary: "Sea and sand.", Description: null, TourType.Group,
        CategoryIds: [], Inclusions: [], Exclusions: [], TermsAndPolicy: null, MinAge: null,
        IsFeatured: false,
        PricingMode.FixedDepartures, DurationDays: 3, DurationNights: 2,
        MinNights: null, MaxNights: null, BasePrice: null, ExtraNightPrice: null, MinLeadDays: null);

    private static readonly CreatePackageCommand Flexible = Fixed with
    {
        PricingMode = PricingMode.FlexibleStay,
        DurationDays = null,
        DurationNights = null,
        MinNights = 2,
        MaxNights = 7,
        BasePrice = 8000,
        ExtraNightPrice = 3000,
        MinLeadDays = 3
    };

    private static string[] FailedFields(CreatePackageCommand command) =>
        new CreatePackageValidator().Validate(command).Errors.Select(e => e.PropertyName).Distinct().ToArray();

    [Fact]
    public void ValidFixedAndFlexibleForms_Pass()
    {
        Assert.Empty(FailedFields(Fixed));
        Assert.Empty(FailedFields(Flexible));
    }

    [Fact]
    public void Fixed_WithoutADuration_FailsOnThoseFields()
    {
        var fields = FailedFields(Fixed with { DurationDays = null, DurationNights = null });

        Assert.Equal(["DurationDays", "DurationNights"], fields);
    }

    [Fact]
    public void Flexible_WithoutPrices_FailsOnThePriceFields_AndIgnoresTheMissingDuration()
    {
        var fields = FailedFields(Flexible with { BasePrice = null, ExtraNightPrice = null });

        Assert.Equal(["BasePrice", "ExtraNightPrice"], fields);
    }

    [Fact]
    public void Flexible_MaxBelowMin_FailsOnMaxNights()
    {
        Assert.Equal(["MaxNights"], FailedFields(Flexible with { MinNights = 5, MaxNights = 3 }));
    }

    [Fact]
    public void Flexible_PriceWithMoreThanTwoDecimals_Fails()
    {
        Assert.Equal(["BasePrice"], FailedFields(Flexible with { BasePrice = 8000.555m }));
    }
}

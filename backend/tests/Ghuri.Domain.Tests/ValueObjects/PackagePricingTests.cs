using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Tests.ValueObjects;

public class PackagePricingTests
{
    [Fact]
    public void FixedDepartures_KeepsTheGivenDuration_AndHasNoFlexibleFields()
    {
        var pricing = PackagePricing.FixedDepartures(3, 2);

        Assert.Equal(PricingMode.FixedDepartures, pricing.Mode);
        Assert.Equal(3, pricing.DurationDays);
        Assert.Equal(2, pricing.DurationNights);
        Assert.Null(pricing.MinNights);
        Assert.Null(pricing.BasePrice);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void FixedDepartures_DurationOutsideOneToSixtyDays_IsRejected(byte days) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PackagePricing.FixedDepartures(days, 0));

    [Fact]
    public void FlexibleStay_DurationIsTheShortestStay()
    {
        var pricing = PackagePricing.FlexibleStay(minNights: 2, maxNights: 7, basePrice: 8000, extraNightPrice: 3000, minLeadDays: 3);

        Assert.Equal(PricingMode.FlexibleStay, pricing.Mode);
        Assert.Equal(2, pricing.DurationNights);
        Assert.Equal(3, pricing.DurationDays);
        Assert.Equal<byte?>(7, pricing.MaxNights);
    }

    [Theory]
    [InlineData(0, 3, 8000, 3000)]  // a stay of zero nights
    [InlineData(4, 3, 8000, 3000)]  // max below min
    [InlineData(1, 60, 8000, 3000)] // longest stay would be 61 days
    [InlineData(2, 5, 0, 3000)]     // free base price
    [InlineData(2, 5, 8000, -1)]    // negative extra night
    public void FlexibleStay_InvalidValues_AreRejected(byte minNights, byte maxNights, int basePrice, int extraNightPrice) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PackagePricing.FlexibleStay(minNights, maxNights, basePrice, extraNightPrice, minLeadDays: 0));

    [Fact]
    public void FlexibleStay_SameNightsForMinAndMax_IsAllowed()
    {
        var pricing = PackagePricing.FlexibleStay(3, 3, 9000, 0, 0);

        Assert.Equal<byte?>(3, pricing.MinNights);
        Assert.Equal<byte?>(3, pricing.MaxNights);
    }
}

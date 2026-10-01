using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Services;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Tests.Services;

/// <summary>
/// The money rules (17-day plan: "Test only what can lose money or seats:
/// … price calculation"). Every expected total is worked out by hand in
/// the comment next to it, so a wrong formula can't hide behind a wrong test.
/// </summary>
public class PriceCalculatorTests
{
    private static TourPackage Package(PackagePricing pricing) => TourPackage.Create(
        "PKG1001",
        new TourPackageDetails(Guid.NewGuid(), "Beach Escape", Slug.Create("Beach Escape"), "Summary", null,
            TourType.Group, [], [], null, null, false, null, null),
        pricing);

    private static readonly TourPackage Fixed = Package(PackagePricing.FixedDepartures(3, 2));

    // Flexible: 2-7 nights, ৳8,000 covers 2 nights, ৳3,000 each extra night.
    private static readonly TourPackage Flexible = Package(PackagePricing.FlexibleStay(2, 7, 8_000, 3_000, 3));

    private static Departure NewDeparture(decimal? singleSupplement = 3_000) =>
        Departure.Create(
            Fixed.Id, new DateOnly(2026, 12, 20), 3,
            adultPrice: 12_000, childPrice: 9_000, infantPrice: 1_000, singleSupplement, totalSeats: 20, bookingCutoffDays: 2);

    // ---------- Fixed departures ----------

    [Fact]
    public void Departure_TwoAdults_IsTwiceTheAdultPrice()
    {
        var price = PriceCalculator.ForDeparture(Fixed, NewDeparture(), Travellers.Create(2), singleRooms: 0);

        var line = Assert.Single(price.Lines);
        Assert.Equal(("Adult", 12_000m, 2, 24_000m), (line.Label, line.UnitPrice, line.Quantity, line.Amount));
        Assert.Equal(24_000, price.Total);
        Assert.Equal("BDT", price.Currency);
    }

    [Fact]
    public void Departure_Family_EachTravellerTypePaysItsOwnPrice()
    {
        // 2 adults × 12,000 + 1 child × 9,000 + 1 infant × 1,000 = 34,000
        var price = PriceCalculator.ForDeparture(Fixed, NewDeparture(), Travellers.Create(2, 1, 1), singleRooms: 0);

        Assert.Equal(["Adult", "Child", "Infant"], price.Lines.Select(l => l.Label));
        Assert.Equal(34_000, price.Total);
    }

    [Fact]
    public void Departure_SingleRoom_AddsTheSupplementPerRoom()
    {
        // 1 adult × 12,000 + 1 single room × 3,000 = 15,000
        var price = PriceCalculator.ForDeparture(Fixed, NewDeparture(), Travellers.Create(1), singleRooms: 1);

        Assert.Equal(15_000, price.Total);
        Assert.Equal("Single room supplement", price.Lines[^1].Label);
    }

    [Fact]
    public void Departure_SingleRoomWhereNoneIsOffered_IsRejected() =>
        Assert.Throws<ArgumentException>(
            () => PriceCalculator.ForDeparture(Fixed, NewDeparture(singleSupplement: null), Travellers.Create(1), singleRooms: 1));

    [Fact]
    public void Departure_MoreSingleRoomsThanAdults_IsRejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PriceCalculator.ForDeparture(Fixed, NewDeparture(), Travellers.Create(1, 1), singleRooms: 2));

    [Fact]
    public void Departure_OfAnotherPackage_IsRejected()
    {
        var otherPackage = Package(PackagePricing.FixedDepartures(3, 2));

        Assert.Throws<ArgumentException>(() => PriceCalculator.ForDeparture(otherPackage, NewDeparture(), Travellers.Create(1), 0));
    }

    // ---------- Flexible stays ----------

    [Theory]
    // nights, expected per person: 8,000 + (nights - 2) × 3,000
    [InlineData(2, 8_000)]   // the minimum: just the base price
    [InlineData(3, 11_000)]  // one extra night
    [InlineData(7, 23_000)]  // the maximum: 5 extra nights
    public void FlexibleStay_BaseCoversMinNights_EachExtraNightAddsItsPrice(int nights, decimal perPerson)
    {
        var price = PriceCalculator.ForFlexibleStay(Flexible, nights, Travellers.Create(1));

        Assert.Equal(perPerson, price.Total);
        Assert.Equal($"Adult · {nights} nights", Assert.Single(price.Lines).Label);
    }

    [Fact]
    public void FlexibleStay_Family_ChildrenPayTheAdultRate_InfantsAreFree()
    {
        // 3 nights = 11,000 per person: 2 adults + 1 child = 33,000; the infant 0
        var price = PriceCalculator.ForFlexibleStay(Flexible, 3, Travellers.Create(2, 1, 1));

        Assert.Equal(33_000, price.Total);
        Assert.Equal(0, price.Lines.Single(l => l.Label == "Infant").Amount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public void FlexibleStay_NightsOutsideTheRange_AreRejected(int nights) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PriceCalculator.ForFlexibleStay(Flexible, nights, Travellers.Create(1)));

    [Fact]
    public void FlexibleStay_OnAFixedPackage_IsRejected() =>
        Assert.Throws<ArgumentException>(() => PriceCalculator.ForFlexibleStay(Fixed, 3, Travellers.Create(1)));
}

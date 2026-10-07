using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Exceptions;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Tests.Entities.Catalog;

public class TourPackageTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid PhotoA = Guid.NewGuid();
    private static readonly Guid PhotoB = Guid.NewGuid();
    private static readonly Guid PhotoC = Guid.NewGuid();

    private static TourPackageDetails Details(string title = "Cox's Bazar Getaway") => new(
        DestinationId: Guid.NewGuid(),
        Title: title,
        Slug: Slug.Create(title),
        Summary: "Sea, sand and sunsets.",
        Description: null,
        TourType: TourType.Group,
        Inclusions: ["Hotel stay", "  ", " Breakfast "],
        Exclusions: [],
        TermsAndPolicy: "",
        MinAge: null,
        IsFeatured: false);

    private static readonly PackagePricing ThreeDays = PackagePricing.FixedDepartures(3, 2);
    private static readonly PackagePricing TwoToSevenNights = PackagePricing.FlexibleStay(2, 7, 8000, 3000, 3);

    private static TourPackage NewPackage(PackagePricing? pricing = null) =>
        TourPackage.Create("PKG1001", Details(), pricing ?? ThreeDays);

    private static ItineraryDayDetails[] Days(int count) =>
        Enumerable.Range(1, count).Select(n => new ItineraryDayDetails($"Day {n}", $"Plan for day {n}")).ToArray();

    /// <summary>A fixed 3-day package with a photo and a 3-day itinerary - everything Publish needs.</summary>
    private static TourPackage ReadyPackage()
    {
        var package = NewPackage();
        package.SetImages([PhotoA]);
        package.SetItinerary(Days(3));
        return package;
    }

    [Fact]
    public void Create_StartsAsDraft_WithCleanedText()
    {
        var package = NewPackage();

        Assert.Equal(PackageStatus.Draft, package.Status);
        Assert.Equal(["Hotel stay", "Breakfast"], package.Inclusions); // blank row dropped, spaces trimmed
        Assert.Null(package.TermsAndPolicy);                           // "" stored as NULL
        Assert.Equal(0, package.PriceFrom);
    }

    [Fact]
    public void Create_Flexible_CopiesPricing_AndPriceFromIsTheBasePrice()
    {
        var package = NewPackage(TwoToSevenNights);

        Assert.Equal(PricingMode.FlexibleStay, package.PricingMode);
        Assert.Equal(3, package.DurationDays);
        Assert.Equal(2, package.DurationNights);
        Assert.Equal<byte?>(7, package.MaxNights);
        Assert.Equal(8000, package.PriceFrom);
    }

    [Fact]
    public void ChangePricing_FlexibleBackToFixed_InDraft_ClearsTheFlexibleFields()
    {
        var package = NewPackage(TwoToSevenNights);

        package.ChangePricing(ThreeDays);

        Assert.Equal(PricingMode.FixedDepartures, package.PricingMode);
        Assert.Null(package.MinNights);
        Assert.Null(package.BasePrice);
        Assert.Equal(0, package.PriceFrom);
    }

    [Fact]
    public void ChangePricing_OtherModeAfterPublishing_IsRejected()
    {
        var package = ReadyPackage();
        package.Publish(Now, hasOpenDeparture: true);

        var error = Assert.Throws<DomainException>(() => package.ChangePricing(TwoToSevenNights));
        Assert.Equal("package_pricing_mode_locked", error.Code);
    }

    [Fact]
    public void SetImages_FirstIsTheCover_AndReorderingKeepsTheSameRows()
    {
        var package = NewPackage();
        package.SetImages([PhotoA, PhotoB]);
        var rowIdsBefore = package.Images.ToDictionary(i => i.FileId, i => i.Id);

        package.SetImages([PhotoB, PhotoA, PhotoC]);

        Assert.Equal([PhotoB, PhotoA, PhotoC], package.Images.Select(i => i.FileId));
        Assert.Equal([0, 1, 2], package.Images.Select(i => i.SortOrder));
        Assert.Equal(rowIdsBefore[PhotoA], package.Images.Single(i => i.FileId == PhotoA).Id);
    }

    [Fact]
    public void SetImages_TheSamePhotoTwice_IsRejected() =>
        Assert.Throws<ArgumentException>(() => NewPackage().SetImages([PhotoA, PhotoA]));

    [Fact]
    public void SetItinerary_NumbersTheDays_AndUpdatesExistingDaysInPlace()
    {
        var package = NewPackage();
        package.SetItinerary(Days(3));
        var day1RowId = package.ItineraryDays[0].Id;

        package.SetItinerary([new ItineraryDayDetails("Arrive", "Check in"), new ItineraryDayDetails("Beach", "Swim")]);

        Assert.Equal([1, 2], package.ItineraryDays.Select(d => (int)d.DayNo));
        Assert.Equal("Arrive", package.ItineraryDays[0].Title);
        Assert.Equal(day1RowId, package.ItineraryDays[0].Id); // same row, so the DB UPDATEs instead of DELETE + INSERT
    }

    [Fact]
    public void SetCategories_IgnoresDuplicates_AndRemovesMissingOnes()
    {
        var package = NewPackage();
        var beach = Guid.NewGuid();
        var family = Guid.NewGuid();
        package.SetCategories([beach, family, beach]);

        package.SetCategories([family]);

        Assert.Equal([family], package.Categories.Select(c => c.CategoryId));
    }

    [Fact]
    public void GetPublishProblems_EmptyPackage_ListsPhotoAndItinerary()
    {
        var problems = NewPackage().GetPublishProblems(hasOpenDeparture: true);

        Assert.Equal(2, problems.Count);
    }

    [Fact]
    public void GetPublishProblems_FixedItineraryShorterThanTheDuration_IsAProblem()
    {
        var package = NewPackage();
        package.SetImages([PhotoA]);
        package.SetItinerary(Days(2));

        Assert.Contains("2 days, but the package lasts 3 days", Assert.Single(package.GetPublishProblems(hasOpenDeparture: true)));
    }

    [Fact]
    public void GetPublishProblems_FlexibleItineraryLongerThanTheLongestStay_IsAProblem()
    {
        var package = NewPackage(TwoToSevenNights);
        package.SetImages([PhotoA]);
        package.SetItinerary(Days(9)); // 7 nights = 8 days at most

        Assert.Single(package.GetPublishProblems(hasOpenDeparture: true));
    }

    [Fact]
    public void GetPublishProblems_FlexibleItineraryOfAnyLengthUpToTheLongestStay_IsFine()
    {
        var package = NewPackage(TwoToSevenNights);
        package.SetImages([PhotoA]);
        package.SetItinerary(Days(1));

        Assert.Empty(package.GetPublishProblems(hasOpenDeparture: true));
    }

    [Fact]
    public void Publish_ReadyPackage_GoesLive()
    {
        var package = ReadyPackage();

        package.Publish(Now, hasOpenDeparture: true);

        Assert.Equal(PackageStatus.Published, package.Status);
        Assert.Equal(Now, package.PublishedAtUtc);
    }

    [Fact]
    public void Publish_WithProblems_IsRejected_AndStaysDraft()
    {
        var package = NewPackage();

        var error = Assert.Throws<DomainException>(() => package.Publish(Now, hasOpenDeparture: true));

        Assert.Equal("package_not_publishable", error.Code);
        Assert.Equal(PackageStatus.Draft, package.Status);
    }

    [Fact]
    public void Publish_Twice_IsRejected()
    {
        var package = ReadyPackage();
        package.Publish(Now, hasOpenDeparture: true);

        Assert.Equal("package_already_published", Assert.Throws<DomainException>(() => package.Publish(Now, hasOpenDeparture: true)).Code);
    }

    [Fact]
    public void Published_RemovingEveryPhoto_IsRejected()
    {
        var package = ReadyPackage();
        package.Publish(Now, hasOpenDeparture: true);

        var error = Assert.Throws<DomainException>(() => package.SetImages([]));
        Assert.Equal("package_must_stay_publishable", error.Code);
    }

    [Fact]
    public void Published_ChangingTheDurationWithoutTheItinerary_IsRejected()
    {
        var package = ReadyPackage();
        package.Publish(Now, hasOpenDeparture: true);

        Assert.Throws<DomainException>(() => package.ChangePricing(PackagePricing.FixedDepartures(4, 3)));
    }

    [Fact]
    public void Archived_CanBeChangedFreely_AndRepublished_KeepingTheFirstPublishDate()
    {
        var package = ReadyPackage();
        package.Publish(Now, hasOpenDeparture: true);
        package.Archive();

        package.SetImages([]); // allowed: archived packages aren't on the site
        package.SetImages([PhotoB]);
        package.Publish(Now.AddDays(5), hasOpenDeparture: true);

        Assert.Equal(PackageStatus.Published, package.Status);
        Assert.Equal(Now, package.PublishedAtUtc);
    }

    [Fact]
    public void Archive_Twice_IsRejected()
    {
        var package = NewPackage();
        package.Archive();

        Assert.Equal("package_already_archived", Assert.Throws<DomainException>(package.Archive).Code);
    }

    [Fact]
    public void GetPublishProblems_FixedWithoutAnOpenDeparture_IsAProblem()
    {
        var problem = Assert.Single(ReadyPackage().GetPublishProblems(hasOpenDeparture: false));

        Assert.Contains("departure", problem);
    }

    [Fact]
    public void GetPublishProblems_FlexibleNeedsNoDeparture()
    {
        var package = NewPackage(TwoToSevenNights);
        package.SetImages([PhotoA]);
        package.SetItinerary(Days(3));

        Assert.Empty(package.GetPublishProblems(hasOpenDeparture: false));
    }

    [Fact]
    public void Publish_FixedWithoutAnOpenDeparture_IsRejected() =>
        Assert.Equal(
            "package_not_publishable",
            Assert.Throws<DomainException>(() => ReadyPackage().Publish(Now, hasOpenDeparture: false)).Code);

    [Fact]
    public void Published_LosingItsLastDeparture_IsAllowed_SoldOutIsNormal()
    {
        var package = ReadyPackage();
        package.Publish(Now, hasOpenDeparture: true);

        // A content change after the last date closed must still work.
        package.SetImages([PhotoB]);

        Assert.Equal(PackageStatus.Published, package.Status);
    }

    [Fact]
    public void SetLowestDeparturePrice_SetsPriceFrom_AndNoneLeftMeansZero()
    {
        var package = NewPackage();

        package.SetLowestDeparturePrice(12_500);
        Assert.Equal(12_500, package.PriceFrom);

        package.SetLowestDeparturePrice(null);
        Assert.Equal(0, package.PriceFrom);
    }

    // ---------- CheckFlexibleStay: 2-7 nights, book at least 3 days ahead ----------

    [Theory]
    [InlineData(4, 2, FlexibleStayBookability.Bookable)]         // 1 Oct + 3 lead days = 4 Oct: the earliest start
    [InlineData(3, 2, FlexibleStayBookability.TooSoon)]          // one day too early
    [InlineData(10, 1, FlexibleStayBookability.NightsOutOfRange)]
    [InlineData(10, 8, FlexibleStayBookability.NightsOutOfRange)]
    [InlineData(10, 7, FlexibleStayBookability.Bookable)]        // exactly MaxNights
    public void CheckFlexibleStay_LeadDaysAndNightsRange(int startDay, int nights, FlexibleStayBookability expected)
    {
        var today = new DateOnly(2026, 10, 1);

        Assert.Equal(expected, NewPackage(TwoToSevenNights).CheckFlexibleStay(today, new DateOnly(2026, 10, startDay), nights));
    }

    [Fact]
    public void EarliestFlexibleStart_OnAFixedPackage_IsRejected() =>
        Assert.Throws<DomainException>(() => NewPackage().EarliestFlexibleStart(new DateOnly(2026, 10, 1)));

    [Fact]
    public void SetLowestDeparturePrice_OnAFlexiblePackage_IsRejected() =>
        Assert.Throws<DomainException>(() => NewPackage(TwoToSevenNights).SetLowestDeparturePrice(5000));
}

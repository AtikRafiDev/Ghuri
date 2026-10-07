using Ghuri.Domain.Entities.Booking;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Exceptions;
using Ghuri.Domain.ValueObjects;
using BookingEntity = Ghuri.Domain.Entities.Booking.Booking;

namespace Ghuri.Domain.Tests.Entities.Booking;

/// <summary>
/// Booking's factories and state machine (17-day plan, Day 8). Money and
/// seats: every expected price is worked out by hand in a comment.
/// </summary>
public class BookingTests
{
    private static readonly DateOnly Today = new(2026, 12, 1);
    private static readonly DateOnly Dec20 = new(2026, 12, 20);
    private static readonly DateTime Now = new(2026, 12, 1, 6, 0, 0, DateTimeKind.Utc);
    private static readonly BookingContact Contact = new("Rahim Uddin", PhoneNumber.Create("01700000000"), "rahim@example.com");

    private static TourPackage Published(PackagePricing pricing, int itineraryDays)
    {
        var package = TourPackage.Create(
            "PKG1001",
            new TourPackageDetails(Guid.NewGuid(), "Beach Escape", Slug.Create("Beach Escape"), "Summary", null,
                TourType.Group, [], [], null, null, false),
            pricing);
        package.SetImages([Guid.NewGuid()]);
        package.SetItinerary(Enumerable.Range(1, itineraryDays).Select(n => new ItineraryDayDetails($"Day {n}", "Plan")).ToArray());
        package.Publish(Now, hasOpenDeparture: true);
        return package;
    }

    // Fixed: 3 days / 2 nights. Departure on the 20th: adult ৳12,000, child ৳9,000, infant ৳1,000, 5 seats.
    private static readonly TourPackage Fixed = Published(PackagePricing.FixedDepartures(3, 2), 3);

    private static Departure NewDeparture(short seats = 5) =>
        Departure.Create(Fixed.Id, Dec20, 3, 12_000, 9_000, 1_000, singleSupplement: null, seats, bookingCutoffDays: 2);

    // Flexible: 2-7 nights, ৳8,000 covers 2 nights, ৳3,000 each extra night, book 3 days ahead.
    private static readonly TourPackage Flexible = Published(PackagePricing.FlexibleStay(2, 7, 8_000, 3_000, 3), 2);

    private static TravellerDetails Adult(string name = "Rahim Uddin", bool lead = false) => new(TravellerType.Adult, name, lead);
    private static TravellerDetails Child(string name = "Ayaan") => new(TravellerType.Child, name, false);
    private static TravellerDetails Infant(string name = "Mim") => new(TravellerType.Infant, name, false);

    // Two adults (one the lead), a child and an infant.
    private static readonly TravellerDetails[] Family = [Adult(lead: true), Adult("Karima Begum"), Child(), Infant()];

    private static BookingEntity FixedBooking(TravellerDetails[]? people = null, Departure? departure = null) =>
        BookingEntity.CreateForDeparture("TB100001", Guid.NewGuid(), Fixed, departure ?? NewDeparture(),
            people ?? Family, Contact, specialRequest: null, BookingSource.Web, Today, Now);

    private static BookingEntity FlexibleBooking(DateOnly? checkIn = null, byte nights = 4) =>
        BookingEntity.CreateFlexible("TB100002", Guid.NewGuid(), Flexible, checkIn ?? Dec20, nights,
            Family, Contact, specialRequest: null, BookingSource.Web, Today, Now);

    // ---------- Creating ----------

    [Fact]
    public void CreateForDeparture_PricesEachTravellerFromTheDeparture()
    {
        var booking = FixedBooking();

        // 2 × 12,000 + 1 × 9,000 + 1 × 1,000 = 34,000
        Assert.Equal((12_000m, 9_000m, 1_000m), (booking.AdultPriceSnapshot, booking.ChildPriceSnapshot, booking.InfantPriceSnapshot));
        Assert.Equal(34_000, booking.SubTotal);
        Assert.Equal(34_000, booking.TotalAmount);
        Assert.Equal(0, booking.PaidAmount);
        Assert.Equal((2, 1, 1), (booking.Adults, booking.Children, booking.Infants));
    }

    [Fact]
    public void CreateForDeparture_CopiesTheDates_AndHoldsTheSeatsFor20Minutes()
    {
        var departure = NewDeparture();

        var booking = FixedBooking(departure: departure);

        Assert.Equal(BookingType.FixedDeparture, booking.BookingType);
        Assert.Equal(departure.Id, booking.DepartureId);
        Assert.Equal((Dec20, new DateOnly(2026, 12, 22), (byte)2), (booking.StartDate, booking.EndDate, booking.Nights));
        Assert.Equal(BookingStatus.PendingPayment, booking.Status);
        Assert.Equal(Now.AddMinutes(20), booking.HoldExpiresAtUtc);
        Assert.Equal(3, booking.SeatsHeld); // 2 adults + 1 child; the infant sits on a lap
    }

    [Fact]
    public void CreateForDeparture_SavesTheTravellers_AndTheFirstStatus()
    {
        var booking = FixedBooking();

        Assert.Equal(4, booking.Travellers.Count);
        Assert.Equal("Rahim Uddin", Assert.Single(booking.Travellers, t => t.IsLead).FullName);
        var first = Assert.Single(booking.History);
        Assert.Equal((null, BookingStatus.PendingPayment), (first.FromStatus, first.ToStatus));
    }

    [Fact]
    public void CreateForDeparture_MoreSeatsThanLeft_IsRefused()
    {
        // 3 seats needed (2 adults + 1 child), only 2 left.
        var error = Assert.Throws<DomainException>(() => FixedBooking(departure: NewDeparture(seats: 2)));
        Assert.Equal("departure_not_bookable", error.Code);
    }

    [Fact]
    public void CreateForDeparture_ADraftPackage_IsRefused()
    {
        var draft = TourPackage.Create(
            "PKG2002",
            new TourPackageDetails(Guid.NewGuid(), "Draft", Slug.Create("Draft"), "Summary", null,
                TourType.Group, [], [], null, null, false),
            PackagePricing.FixedDepartures(3, 2));
        var departure = Departure.Create(draft.Id, Dec20, 3, 12_000, 9_000, 1_000, null, 5, 2);

        var error = Assert.Throws<DomainException>(() => BookingEntity.CreateForDeparture(
            "TB100003", Guid.NewGuid(), draft, departure, Family, Contact, null, BookingSource.Web, Today, Now));
        Assert.Equal("package_not_for_sale", error.Code);
    }

    [Fact]
    public void CreateFlexible_PricesThePerPersonRate_AndWorksOutCheckOut()
    {
        var booking = FlexibleBooking(nights: 4);

        // Per person: 8,000 for 2 nights + 2 extra nights × 3,000 = 14,000.
        // 2 adults + 1 child at 14,000, infant free = 42,000.
        Assert.Equal((14_000m, 14_000m, 0m), (booking.AdultPriceSnapshot, booking.ChildPriceSnapshot, booking.InfantPriceSnapshot));
        Assert.Equal(42_000, booking.TotalAmount);
        Assert.Equal(BookingType.FlexibleStay, booking.BookingType);
        Assert.Null(booking.DepartureId);
        Assert.Equal(new DateOnly(2026, 12, 24), booking.EndDate); // in on the 20th, 4 nights, out on the 24th
        Assert.Equal(0, booking.SeatsHeld); // no departure, no seats
    }

    [Fact]
    public void CreateFlexible_StartingTooSoon_IsRefused()
    {
        // 3 days' notice needed: from the 1st, the earliest check-in is the 4th.
        var error = Assert.Throws<DomainException>(() => FlexibleBooking(checkIn: new DateOnly(2026, 12, 2)));
        Assert.Equal("stay_not_bookable", error.Code);
    }

    [Fact]
    public void CreateFlexible_NightsOutsideTheRange_AreRefused() =>
        Assert.Equal("stay_not_bookable", Assert.Throws<DomainException>(() => FlexibleBooking(nights: 8)).Code);

    // ---------- Who's travelling ----------

    [Fact]
    public void NoLeadTraveller_IsRejected() =>
        Assert.Throws<ArgumentException>(() => FixedBooking([Adult(), Adult("Karima Begum")]));

    [Fact]
    public void TwoLeadTravellers_AreRejected() =>
        Assert.Throws<ArgumentException>(() => FixedBooking([Adult(lead: true), Adult("Karima Begum", lead: true)]));

    [Fact]
    public void AChildAsTheLead_IsRejected() =>
        Assert.Throws<ArgumentException>(() => FixedBooking([Adult(), new TravellerDetails(TravellerType.Child, "Ayaan", true)]));

    [Fact]
    public void MoreInfantsThanAdults_AreRejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => FixedBooking([Adult(lead: true), Infant(), Infant("Mira")]));

    // ---------- State machine ----------

    [Fact]
    public void Expire_AfterTheWindow_EndsTheHold_AndIsRecorded()
    {
        var booking = FixedBooking();

        booking.Expire(Now.AddMinutes(20));

        Assert.Equal(BookingStatus.Expired, booking.Status);
        Assert.Null(booking.HoldExpiresAtUtc);
        var last = booking.History[^1];
        Assert.Equal((BookingStatus.PendingPayment, BookingStatus.Expired), (last.FromStatus, last.ToStatus));
        Assert.Null(last.ChangedBy); // a background job did it
    }

    [Fact]
    public void Expire_WhileTheWindowIsOpen_IsRefused()
    {
        var booking = FixedBooking();

        var error = Assert.Throws<DomainException>(() => booking.Expire(Now.AddMinutes(19)));

        Assert.Equal("booking_hold_open", error.Code);
        Assert.Equal(BookingStatus.PendingPayment, booking.Status);
    }

    /// <summary>The whole price arrives - what a validated payment does before confirming.</summary>
    private static BookingEntity Paid(BookingEntity booking)
    {
        booking.RecordPayment(booking.TotalAmount - booking.PaidAmount);
        return booking;
    }

    [Fact]
    public void Confirm_PendingBooking_IsConfirmed_AndTheHoldIsCleared()
    {
        var booking = Paid(FixedBooking());

        booking.Confirm(Now.AddMinutes(5));

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Null(booking.HoldExpiresAtUtc);
        Assert.Equal(2, booking.History.Count);
    }

    [Fact]
    public void Confirming_RaisesBookingConfirmed_ForTheVoucherEmail()
    {
        var booking = Paid(FixedBooking());

        booking.Confirm(Now.AddMinutes(5));

        Assert.Equal(new BookingConfirmed(booking.Id), Assert.Single(booking.DomainEvents));
    }

    [Fact]
    public void ConfirmingAfterExpiry_RaisesBookingConfirmed_Too()
    {
        var booking = FixedBooking();
        booking.Expire(Now.AddMinutes(25));

        Paid(booking).ConfirmAfterExpiry(Now.AddMinutes(30));

        Assert.IsType<BookingConfirmed>(Assert.Single(booking.DomainEvents));
    }

    [Fact]
    public void Confirm_BeforeItIsPaidInFull_IsRefused()
    {
        var booking = FixedBooking();
        booking.RecordPayment(1_000); // part of the price only

        Assert.Equal("booking_not_paid", Assert.Throws<DomainException>(() => booking.Confirm(Now.AddMinutes(5))).Code);
        Assert.Equal(BookingStatus.PendingPayment, booking.Status);
    }

    [Fact]
    public void Confirm_AfterItExpired_IsRefused()
    {
        var booking = Paid(FixedBooking());
        booking.Expire(Now.AddMinutes(25));

        Assert.Equal("booking_not_pending", Assert.Throws<DomainException>(() => booking.Confirm(Now.AddMinutes(26))).Code);
    }

    [Fact]
    public void RecordPayment_AddsUp_WhateverTheStatus()
    {
        var booking = FixedBooking();
        booking.Expire(Now.AddMinutes(25)); // money can arrive after expiry - it must still show

        booking.RecordPayment(30_000);
        booking.RecordPayment(4_000);

        Assert.Equal(34_000m, booking.PaidAmount);
        Assert.True(booking.IsPaidInFull);
    }

    [Fact]
    public void RecordPayment_MoreThanThePrice_IsRefused()
    {
        var booking = Paid(FixedBooking());

        Assert.Equal("booking_overpaid", Assert.Throws<DomainException>(() => booking.RecordPayment(1)).Code);
        Assert.Equal(34_000m, booking.PaidAmount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RecordPayment_OfNothing_IsRejected(decimal amount) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => FixedBooking().RecordPayment(amount));

    [Fact]
    public void ConfirmAfterExpiry_APaidExpiredBooking_IsConfirmed_WithANote()
    {
        var booking = FixedBooking();
        booking.Expire(Now.AddMinutes(25));
        Paid(booking);

        booking.ConfirmAfterExpiry(Now.AddMinutes(30));

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal((BookingStatus.Expired, BookingStatus.Confirmed, "Paid after the payment window ended."),
            (booking.History[^1].FromStatus, booking.History[^1].ToStatus, booking.History[^1].Note));
    }

    [Fact]
    public void ConfirmAfterExpiry_UnpaidOrNotExpired_IsRefused()
    {
        var unpaid = FixedBooking();
        unpaid.Expire(Now.AddMinutes(25));
        Assert.Equal("booking_not_paid", Assert.Throws<DomainException>(() => unpaid.ConfirmAfterExpiry(Now.AddMinutes(30))).Code);

        var pending = Paid(FixedBooking());
        Assert.Equal("booking_not_expired", Assert.Throws<DomainException>(() => pending.ConfirmAfterExpiry(Now.AddMinutes(5))).Code);
    }

    [Fact]
    public void Expire_AConfirmedBooking_IsRefused()
    {
        var booking = Paid(FixedBooking());
        booking.Confirm(Now.AddMinutes(5));

        Assert.Equal("booking_not_pending", Assert.Throws<DomainException>(() => booking.Expire(Now.AddHours(1))).Code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cancel_PendingOrConfirmed_RecordsWhyAndWho(bool confirmFirst)
    {
        var booking = FixedBooking();
        if (confirmFirst)
            Paid(booking).Confirm(Now.AddMinutes(5));
        var staffId = Guid.NewGuid();

        booking.Cancel(Now.AddMinutes(10), "  Hotel not available  ", staffId);

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal("Hotel not available", booking.CancelReason);
        Assert.Equal(Now.AddMinutes(10), booking.CancelledAtUtc);
        Assert.Equal((staffId, "Hotel not available"), (booking.History[^1].ChangedBy, booking.History[^1].Note));
    }

    [Fact]
    public void Cancel_AnExpiredBooking_IsRefused()
    {
        var booking = FixedBooking();
        booking.Expire(Now.AddMinutes(20));

        Assert.Equal("booking_not_cancellable",
            Assert.Throws<DomainException>(() => booking.Cancel(Now.AddMinutes(30), "Changed my mind", null)).Code);
    }

    [Fact]
    public void Cancel_WithoutAReason_IsRejected() =>
        Assert.Throws<ArgumentException>(() => FixedBooking().Cancel(Now, "  ", null));
}

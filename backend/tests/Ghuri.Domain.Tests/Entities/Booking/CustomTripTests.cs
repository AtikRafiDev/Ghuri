using Ghuri.Domain.Entities.Booking;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Exceptions;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Tests.Entities.Booking;

/// <summary>
/// The custom trip's rules (17-day plan, Day 13): dates worked out from the
/// stops, a quote that is always the sum of its lines, and a life that only
/// moves the allowed ways.
/// </summary>
public class CustomTripTests
{
    private static readonly DateOnly Today = new(2026, 12, 1);
    private static readonly DateTime Now = new(2026, 12, 1, 6, 0, 0, DateTimeKind.Utc);
    private static readonly Guid CoxsBazar = Guid.NewGuid();
    private static readonly Guid Sylhet = Guid.NewGuid();
    private static readonly Guid Staff = Guid.NewGuid();

    /// <summary>The plan's example: Cox's Bazar 3N → Sylhet 2N, from 20 Dec, 2 adults.</summary>
    private static CustomTrip Submit(DateOnly? start = null, IReadOnlyList<CustomTripLegRequest>? legs = null) =>
        CustomTrip.Submit(
            "CT1001", Guid.NewGuid(), start ?? new DateOnly(2026, 12, 20), Travellers.Create(2), HotelLevel.Standard,
            budgetPerPerson: 25_000, notes: "  Sea-facing room  ",
            new BookingContact("Rahim Uddin", PhoneNumber.Create("01712345678"), "rahim@example.com"),
            legs ?? [new(CoxsBazar, 3, TransferMode.Bus), new(Sylhet, 2, TransferMode.Air)],
            Today, Now);

    private static readonly QuoteLineInput[] Lines =
    [
        new(QuoteLineCategory.Hotel, "Sea Pearl 3N + Rose View 2N, 1 room", 38_000),
        new(QuoteLineCategory.Transport, "AC bus Dhaka-Cox's, flight Cox's-Sylhet", 21_000),
        new(QuoteLineCategory.Meals, "Breakfast daily", 3_000)
    ];

    // ---------- Submitting ----------

    [Fact]
    public void Submit_WorksOutEveryStopsDates_FromTheStartDate()
    {
        var trip = Submit();

        Assert.Equal((CustomTripStatus.Submitted, new DateOnly(2026, 12, 25), (byte)5), (trip.Status, trip.EndDate, trip.TotalNights));
        Assert.Equal(
            new[] { (1, new DateOnly(2026, 12, 20), new DateOnly(2026, 12, 23), TransferMode.Bus), (2, new DateOnly(2026, 12, 23), new DateOnly(2026, 12, 25), TransferMode.None) },
            trip.Legs.Select(l => ((int)l.Sequence, l.CheckInDate, l.CheckOutDate, l.TransferToNext)));
        Assert.Equal("Sea-facing room", trip.Notes);
        Assert.Equal(new CustomTripSubmitted(trip.Id), Assert.Single(trip.DomainEvents));
    }

    [Fact]
    public void Submit_TooSoon_IsRefused() =>
        Assert.Equal("trip_start_too_soon",
            Assert.Throws<DomainException>(() => Submit(start: Today.AddDays(CustomTrip.MinLeadDays - 1))).Code);

    [Fact]
    public void Submit_ExactlyTheLeadTime_IsFine() =>
        Assert.Equal(CustomTripStatus.Submitted, Submit(start: Today.AddDays(CustomTrip.MinLeadDays)).Status);

    [Fact]
    public void Submit_NoStops_OrTooMany_IsRefused()
    {
        Assert.Equal("trip_legs_count", Assert.Throws<DomainException>(() => Submit(legs: [])).Code);
        var eleven = Enumerable.Range(0, CustomTrip.MaxLegs + 1).Select(_ => new CustomTripLegRequest(CoxsBazar, 1, TransferMode.Bus)).ToList();
        Assert.Equal("trip_legs_count", Assert.Throws<DomainException>(() => Submit(legs: eleven)).Code);
    }

    [Fact]
    public void Submit_AStopWithoutNights_OrATooLongTrip_IsRefused()
    {
        Assert.Equal("trip_leg_nights", Assert.Throws<DomainException>(() => Submit(legs: [new(CoxsBazar, 0, TransferMode.Bus)])).Code);
        Assert.Equal("trip_too_long", Assert.Throws<DomainException>(() =>
            Submit(legs: [new(CoxsBazar, 30, TransferMode.Bus), new(Sylhet, 30, TransferMode.Bus), new(CoxsBazar, 1, TransferMode.None)])).Code);
    }

    // ---------- Quoting ----------

    [Fact]
    public void Quote_TheTotalIsTheSumOfTheLines_AndItRunsForTheGivenDays()
    {
        var trip = Submit();
        trip.ClearDomainEvents();

        trip.Quote("Day 1: bus to Cox's Bazar…", Lines, validDays: 3, Staff, Now);

        Assert.Equal((CustomTripStatus.Quoted, 62_000m, 1, (DateTime?)Now.AddDays(3), (Guid?)Staff),
            (trip.Status, trip.QuoteTotal, trip.QuoteVersion, trip.QuoteExpiresAtUtc, trip.QuotedBy));
        Assert.Equal(3, trip.QuoteLines.Count);
        Assert.Equal(new CustomTripQuoted(trip.Id, 1), Assert.Single(trip.DomainEvents));
    }

    [Fact]
    public void AReQuote_ReplacesTheLines_AndRestartsTheClock()
    {
        var trip = Submit();
        trip.Quote("First plan", Lines, 3, Staff, Now);

        trip.Quote("Cheaper plan", [new(QuoteLineCategory.Hotel, "Budget hotels", 30_000)], 5, Staff, Now.AddDays(1));

        Assert.Equal((2, 30_000m, "Cheaper plan", (DateTime?)Now.AddDays(6)), (trip.QuoteVersion, trip.QuoteTotal, trip.QuoteItinerary, trip.QuoteExpiresAtUtc));
        Assert.Equal("Budget hotels", Assert.Single(trip.QuoteLines).Description);
    }

    [Fact]
    public void AQuote_WithoutLines_WithAFreeLine_OrAWrongValidity_IsRefused()
    {
        var trip = Submit();

        Assert.Equal("quote_lines_count", Assert.Throws<DomainException>(() => trip.Quote("x", [], 3, Staff, Now)).Code);
        Assert.Equal("quote_line_invalid", Assert.Throws<DomainException>(() =>
            trip.Quote("x", [new(QuoteLineCategory.Guide, "Guide", 0)], 3, Staff, Now)).Code);
        Assert.Equal("quote_validity", Assert.Throws<DomainException>(() => trip.Quote("x", Lines, 0, Staff, Now)).Code);
        Assert.Equal(CustomTripStatus.Submitted, trip.Status);
    }

    [Fact]
    public void ACancelledOrRejectedTrip_CantBeQuoted()
    {
        var cancelled = Submit();
        cancelled.Cancel(null, Now);
        var rejected = Submit();
        rejected.Reject("No hotels in December", Now);

        Assert.Equal("trip_not_quotable", Assert.Throws<DomainException>(() => cancelled.Quote("x", Lines, 3, Staff, Now)).Code);
        Assert.Equal("trip_not_quotable", Assert.Throws<DomainException>(() => rejected.Quote("x", Lines, 3, Staff, Now)).Code);
    }

    // ---------- Expiry ----------

    [Fact]
    public void ExpireQuote_OnlyOnceItIsOverdue_AndARunTwiceChangesNothing()
    {
        var trip = Submit();
        trip.Quote("Plan", Lines, 3, Staff, Now);

        trip.ExpireQuote(Now.AddDays(2)); // still valid
        Assert.Equal(CustomTripStatus.Quoted, trip.Status);

        trip.ExpireQuote(Now.AddDays(3));
        trip.ExpireQuote(Now.AddDays(4));
        Assert.Equal((CustomTripStatus.Expired, (DateTime?)Now.AddDays(3)), (trip.Status, trip.ExpiredAtUtc));
    }

    [Fact]
    public void AnExpiredQuote_CanBeQuotedAgain()
    {
        var trip = Submit();
        trip.Quote("Plan", Lines, 1, Staff, Now);
        trip.ExpireQuote(Now.AddDays(2));

        trip.Quote("New dates, same plan", Lines, 3, Staff, Now.AddDays(2));

        Assert.Equal((CustomTripStatus.Quoted, 2, (DateTime?)null), (trip.Status, trip.QuoteVersion, trip.ExpiredAtUtc));
    }

    // ---------- Reject / cancel ----------

    [Fact]
    public void Reject_KeepsTheReason_AndTellsTheCustomer()
    {
        var trip = Submit();
        trip.ClearDomainEvents();

        trip.Reject("  No hotels free that week  ", Now);

        Assert.Equal((CustomTripStatus.Rejected, "No hotels free that week"), (trip.Status, trip.RejectReason));
        Assert.Equal(new CustomTripRejected(trip.Id), Assert.Single(trip.DomainEvents));
    }

    [Fact]
    public void Cancel_WorksBeforeAccepting_ButNotAfterARejection()
    {
        var quoted = Submit();
        quoted.Quote("Plan", Lines, 3, Staff, Now);
        quoted.Cancel("Found cheaper elsewhere", Now);
        Assert.Equal((CustomTripStatus.Cancelled, "Found cheaper elsewhere"), (quoted.Status, quoted.CancelReason));

        var rejected = Submit();
        rejected.Reject("No", Now);
        Assert.Equal("trip_not_cancellable", Assert.Throws<DomainException>(() => rejected.Cancel(null, Now)).Code);
    }
}

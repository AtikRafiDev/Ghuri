using Ghuri.Application.Features.Booking.Queries.GetBookingQuote;

namespace Ghuri.Application.Tests.Features.Booking;

/// <summary>
/// Bad traveller counts must come back as a 400 with field errors - never
/// reach Travellers.Create, which would throw (a 500).
/// </summary>
public class GetBookingQuoteValidatorTests
{
    private static string[] FailedFields(GetBookingQuoteQuery query) =>
        new GetBookingQuoteValidator().Validate(query).Errors.Select(e => e.PropertyName).Distinct().ToArray();

    private static GetBookingQuoteQuery Query(int adults, int children = 0, int infants = 0, int singleRooms = 0) =>
        new("beach-escape", Guid.NewGuid(), null, null, adults, children, infants, singleRooms);

    [Fact]
    public void AnOrdinaryFamily_Passes() => Assert.Empty(FailedFields(Query(2, 1, 1)));

    [Fact]
    public void NoAdult_FailsOnAdults() => Assert.Equal(["Adults"], FailedFields(Query(0)));

    [Fact]
    public void MoreInfantsThanAdults_FailsOnInfants() => Assert.Equal(["Infants"], FailedFields(Query(1, infants: 2)));

    [Fact]
    public void MoreThanTwentyPeople_FailsOnTravellers() => Assert.Equal(["Travellers"], FailedFields(Query(15, 6)));

    [Fact]
    public void MoreSingleRoomsThanAdults_FailsOnSingleRooms() =>
        Assert.Equal(["SingleRooms"], FailedFields(Query(1, singleRooms: 2)));
}

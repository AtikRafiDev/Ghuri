using Ghuri.Application.Features.Booking.Commands.CreateBooking;
using Ghuri.Application.Tests.Features.Identity;
using Ghuri.Domain.Enums;

namespace Ghuri.Application.Tests.Features.Booking;

/// <summary>
/// A bad checkout form must come back as a 400 with field errors the page can
/// show - never reach Booking, whose own checks would throw (a 500).
/// </summary>
public class CreateBookingValidatorTests
{
    private static readonly FakeClock Clock = new(new DateTime(2026, 12, 1, 6, 0, 0, DateTimeKind.Utc));

    private static readonly TravellerInput Lead = new(TravellerType.Adult, "Rahim Uddin", IsLead: true);
    private static readonly TravellerInput Spouse = new(TravellerType.Adult, "Karima Begum", IsLead: false);
    private static readonly TravellerInput Baby = new(TravellerType.Infant, "Mim", IsLead: false);

    private static CreateBookingCommand Command(IReadOnlyList<TravellerInput>? travellers = null, string phone = "01712345678",
        string? email = "rahim@example.com", string? key = "b6c1e2f0-checkout-1") =>
        new("beach-escape", Guid.NewGuid(), null, null, travellers ?? [Lead, Spouse, Baby], "Rahim Uddin", phone, email, null)
        {
            IdempotencyKey = key
        };

    private static string[] FailedFields(CreateBookingCommand command) =>
        new CreateBookingValidator(Clock).Validate(command).Errors.Select(e => e.PropertyName).Distinct().ToArray();

    [Fact]
    public void AnOrdinaryFamily_Passes() => Assert.Empty(FailedFields(Command()));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("has spaces")]
    public void AMissingOrOddIdempotencyKey_FailsOnIdempotencyKey(string? key) =>
        Assert.Equal(["IdempotencyKey"], FailedFields(Command(key: key)));

    [Fact]
    public void NoLeadTraveller_FailsOnTravellers() =>
        Assert.Equal(["Travellers"], FailedFields(Command([Spouse with { IsLead = false }, Spouse])));

    [Fact]
    public void AnInfantAsTheLead_FailsOnTravellers() =>
        Assert.Equal(["Travellers"], FailedFields(Command([Spouse, Baby with { IsLead = true }])));

    [Fact]
    public void MoreInfantsThanAdults_FailsOnTravellers() =>
        Assert.Equal(["Travellers"], FailedFields(Command([Lead, Baby, Baby with { FullName = "Mira" }])));

    [Fact]
    public void ATravellerWithoutAName_FailsOnThatTravellersName() =>
        Assert.Equal(["Travellers[1].FullName"], FailedFields(Command([Lead, Spouse with { FullName = " " }])));

    [Fact]
    public void ABirthDateInTheFuture_FailsOnIt() =>
        Assert.Equal(["Travellers[1].DateOfBirth"],
            FailedFields(Command([Lead, Spouse with { DateOfBirth = new DateOnly(2027, 1, 1) }])));

    [Fact]
    public void ANationalityThatIsNotATwoLetterCode_FailsOnIt() =>
        Assert.Equal(["Travellers[0].Nationality"], FailedFields(Command([Lead with { Nationality = "Bangladesh" }])));

    [Fact]
    public void AnInvalidPhone_FailsOnContactPhone() => Assert.Equal(["ContactPhone"], FailedFields(Command(phone: "12345")));

    [Fact]
    public void AnInvalidEmail_FailsOnContactEmail() => Assert.Equal(["ContactEmail"], FailedFields(Command(email: "not-an-email")));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NoEmail_FailsOnContactEmail(string? email) =>
        Assert.Equal(["ContactEmail"], FailedFields(Command(email: email))); // the gateway and the voucher need it
}

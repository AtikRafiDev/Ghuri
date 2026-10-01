using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Tests.ValueObjects;

public class TravellersTests
{
    [Fact]
    public void Infants_DontTakeASeat()
    {
        var family = Travellers.Create(adults: 2, children: 1, infants: 1);

        Assert.Equal(4, family.People);
        Assert.Equal(3, family.Seats);
    }

    [Theory]
    [InlineData(0, 1, 0)]  // no adult
    [InlineData(1, -1, 0)] // negative
    [InlineData(1, 0, 2)]  // two infants, one adult
    [InlineData(15, 6, 0)] // 21 people
    public void Impossible_Groups_AreRejected(int adults, int children, int infants) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Travellers.Create(adults, children, infants));

    [Fact]
    public void Exactly_TheMaximum_IsAllowed() =>
        Assert.Equal(Travellers.MaxPerBooking, Travellers.Create(10, 5, 5).People);
}

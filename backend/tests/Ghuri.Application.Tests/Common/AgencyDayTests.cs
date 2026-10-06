using Ghuri.Application.Common;

namespace Ghuri.Application.Tests.Common;

/// <summary>
/// The dashboard's daily chart puts each payment and booking on a Dhaka
/// (UTC+6) calendar day. The edges are where it would go wrong.
/// </summary>
public class AgencyDayTests
{
    [Fact]
    public void Of_LateEveningUtc_IsAlreadyTomorrowInDhaka()
    {
        var utc = new DateTime(2026, 10, 6, 18, 30, 0, DateTimeKind.Utc); // 00:30 on 7 Oct in Dhaka

        Assert.Equal(new DateOnly(2026, 10, 7), AgencyDay.Of(utc));
    }

    [Fact]
    public void Of_OneTickBeforeDhakaMidnight_IsStillToday()
    {
        var utc = AgencyDay.StartUtc(new DateOnly(2026, 10, 7)).AddTicks(-1);

        Assert.Equal(new DateOnly(2026, 10, 6), AgencyDay.Of(utc));
    }

    [Fact]
    public void Of_StartUtc_RoundTrips()
    {
        var day = new DateOnly(2026, 12, 31);

        Assert.Equal(day, AgencyDay.Of(AgencyDay.StartUtc(day)));
    }
}

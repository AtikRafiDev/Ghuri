using Ghuri.Domain.Entities.Booking;

namespace Ghuri.Domain.Tests.Entities.Booking;

/// <summary>Which refund % applies (Day 11). Money: a day off in either direction is a different refund.</summary>
public class CancellationPolicyTests
{
    private static readonly Guid PackageId = Guid.NewGuid();

    /// <summary>The seeded global policy: 30+ days 100% · 15+ 50% · 7+ 25% · less 0%.</summary>
    private static readonly CancellationPolicy[] Global =
    [
        CancellationPolicy.Create(null, 0, 0),
        CancellationPolicy.Create(null, 30, 100), // order in the list doesn't matter
        CancellationPolicy.Create(null, 7, 25),
        CancellationPolicy.Create(null, 15, 50)
    ];

    [Theory]
    [InlineData(90, 100)]
    [InlineData(30, 100)]
    [InlineData(29, 50)]
    [InlineData(15, 50)]
    [InlineData(14, 25)]
    [InlineData(7, 25)]
    [InlineData(6, 0)]
    [InlineData(1, 0)]
    public void TheBiggestRuleThatFits_Wins(int daysBefore, int expectedPercent) =>
        Assert.Equal(expectedPercent, CancellationPolicy.RefundPercentFor(Global, PackageId, daysBefore));

    [Fact]
    public void APackagesOwnRules_ReplaceTheGlobalOnes_Completely()
    {
        CancellationPolicy[] rules = [.. Global, CancellationPolicy.Create(PackageId, 10, 80)];

        Assert.Equal(80, CancellationPolicy.RefundPercentFor(rules, PackageId, 40)); // not the global 100%
        Assert.Equal(0, CancellationPolicy.RefundPercentFor(rules, PackageId, 5)); // its own rules don't cover 5 days
        Assert.Equal(100, CancellationPolicy.RefundPercentFor(rules, Guid.NewGuid(), 40)); // other packages: global
    }

    [Fact]
    public void NoRulesAtAll_RefundsNothing() =>
        Assert.Equal(0, CancellationPolicy.RefundPercentFor([], PackageId, 100));

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void APercentOutside0To100_IsRejected(decimal percent) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CancellationPolicy.Create(null, 0, percent));
}

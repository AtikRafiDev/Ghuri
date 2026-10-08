using Ghuri.Domain.Common;
using Ghuri.Domain.Services;

namespace Ghuri.Domain.Tests.Services;

/// <summary>Unique sort order numbers: placing an item on a taken number moves the others along (2026-10-08).</summary>
public class DisplayOrderTests
{
    /// <summary>The smallest possible ISortable - the rule doesn't care what is being sorted.</summary>
    private sealed class Item(int sortOrder) : ISortable
    {
        public int SortOrder { get; private set; } = sortOrder;

        public void MoveTo(int sortOrder) => SortOrder = sortOrder;
    }

    private static List<Item> Items(params int[] sortOrders) => sortOrders.Select(n => new Item(n)).ToList();

    private static int[] Numbers(List<Item> items) => items.Select(i => i.SortOrder).ToArray();

    [Fact]
    public void AFreeNumber_MovesNobody()
    {
        var others = Items(10, 20, 30);

        DisplayOrder.MakeRoom(15, others);

        Assert.Equal([10, 20, 30], Numbers(others));
    }

    [Fact]
    public void ATakenNumber_MovesThatItemUpByOne()
    {
        var others = Items(10, 20, 30);

        DisplayOrder.MakeRoom(20, others);

        Assert.Equal([10, 21, 30], Numbers(others));
    }

    [Fact]
    public void ARunOfTakenNumbers_MovesAlong_OnlyUpToTheFirstGap()
    {
        var others = Items(2, 3, 4, 10);

        DisplayOrder.MakeRoom(2, others);

        Assert.Equal([3, 4, 5, 10], Numbers(others)); // 5 was free - 10 never moves
    }

    [Fact]
    public void NumbersAlreadyShared_AreSeparatedToo()
    {
        // Saved before numbers were unique - e.g. two destinations both on 2.
        var others = Items(2, 2, 3);

        DisplayOrder.MakeRoom(2, others);

        Assert.Equal([3, 4, 5], Numbers(others));
    }

    [Fact]
    public void TheOthersCanComeInAnyOrder()
    {
        var others = Items(30, 21, 20);

        DisplayOrder.MakeRoom(20, others);

        Assert.Equal([30, 22, 21], Numbers(others));
    }

    [Theory]
    [InlineData(null, DisplayOrder.Step)] // the first item
    [InlineData(90, 90 + DisplayOrder.Step)]
    public void AfterLast_LeavesAGapOfOneStep(int? last, int expected) =>
        Assert.Equal(expected, DisplayOrder.AfterLast(last));
}

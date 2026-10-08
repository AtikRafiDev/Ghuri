using Ghuri.Domain.Common;

namespace Ghuri.Domain.Services;

/// <summary>
/// Keeps the admin's sort order numbers unique (decided 2026-10-08): no two
/// destinations share a number, so the site shows exactly the order the
/// admin typed - never a tie broken by name.
/// </summary>
/// <remarks>
/// A domain service, like PriceCalculator: a rule about many objects at
/// once, so it belongs to no single entity. No database - easy to test.
/// </remarks>
public static class DisplayOrder
{
    /// <summary>
    /// The gap left after the last item (the demo data uses 10, 20, 30...):
    /// room to slot one in between later without moving anything.
    /// </summary>
    public const int Step = 10;

    /// <summary>The number for an item placed at the end: Step after the last one, or Step when there is none.</summary>
    public static int AfterLast(int? lastSortOrder) => (lastSortOrder ?? 0) + Step;

    /// <summary>
    /// Frees <paramref name="sortOrder"/> for an item being added or moved
    /// there. The item already on that number moves up by one; if THAT
    /// number is taken too, its item moves up as well - and so on, only as
    /// far as the first free number. Everything after a gap stays put.
    /// </summary>
    /// <param name="sortOrder">The number the item being placed will get.</param>
    /// <param name="others">The OTHER items (never the one being placed) - only those on sortOrder or above matter.</param>
    /// <example>Placing at 20 when others have 20, 21 and 30: they become 21, 22 and 30.</example>
    public static void MakeRoom<T>(int sortOrder, IEnumerable<T> others) where T : ISortable
    {
        var taken = sortOrder;
        foreach (var item in others.Where(o => o.SortOrder >= sortOrder).OrderBy(o => o.SortOrder).ToList())
        {
            if (item.SortOrder > taken)
                break; // a free number - nothing from here on is in the way

            taken++;
            item.MoveTo(taken);
        }
    }
}

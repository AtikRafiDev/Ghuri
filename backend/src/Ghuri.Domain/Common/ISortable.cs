namespace Ghuri.Domain.Common;

/// <summary>
/// Shown in an order the admin picks - lower SortOrder first. No two share
/// a number: DisplayOrder (Domain/Services) moves the others along to make room.
/// </summary>
public interface ISortable
{
    int SortOrder { get; }

    /// <summary>Gives this item another number - DisplayOrder calls it when another item takes this one's place.</summary>
    void MoveTo(int sortOrder);
}

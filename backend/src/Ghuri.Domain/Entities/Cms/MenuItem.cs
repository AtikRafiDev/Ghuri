using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;

namespace Ghuri.Domain.Entities.Cms;

/// <summary>One header or footer navigation link, optionally nested under a parent (blueprint: cms.MenuItems [A]).</summary>
public sealed class MenuItem : AggregateRoot, IAuditable
{
    public MenuLocation Location { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;

    /// <summary>Null means a top-level item; otherwise this nests under another MenuItem - self-referencing FK.</summary>
    public Guid? ParentId { get; private set; }

    public int SortOrder { get; private set; }

    private MenuItem()
    {
    }

    public static MenuItem Create(MenuLocation location, string label, string url, Guid? parentId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        return new MenuItem
        {
            Location = location,
            Label = label,
            Url = url,
            ParentId = parentId,
            SortOrder = 0
        };
    }
}

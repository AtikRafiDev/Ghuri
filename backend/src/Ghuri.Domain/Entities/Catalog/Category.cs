using Ghuri.Domain.Common;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Entities.Catalog;

/// <summary>Tour types - Beach, Hill, Honeymoon, Umrah... (blueprint: catalog.Categories [A][S]).</summary>
public sealed class Category : AggregateRoot, IAuditable, ISoftDeletable
{
    public string Name { get; private set; } = string.Empty;
    public Slug Slug { get; private set; } = null!;

    /// <summary>A lucide-react icon name the frontend draws, e.g. "umbrella", "mountain".</summary>
    public string? Icon { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    private Category()
    {
    }

    public static Category Create(string name, Slug slug, string? icon = null, int sortOrder = 0)
    {
        var category = new Category();
        category.Update(name, slug, icon, sortOrder);
        return category;
    }

    public void Update(string name, Slug slug, string? icon, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(slug);

        Name = name.Trim();
        Slug = slug;
        Icon = string.IsNullOrWhiteSpace(icon) ? null : icon.Trim();
        SortOrder = sortOrder;
    }

    public void MarkDeleted(DateTime nowUtc)
    {
        IsDeleted = true;
        DeletedAtUtc = nowUtc;
    }
}

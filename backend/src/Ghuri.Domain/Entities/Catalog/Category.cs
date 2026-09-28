using Ghuri.Domain.Common;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Entities.Catalog;

/// <summary>Tour types - Beach, Hill, Honeymoon, Umrah... (blueprint: catalog.Categories [A][S]).</summary>
public sealed class Category : AggregateRoot, IAuditable, ISoftDeletable
{
    public string Name { get; private set; } = string.Empty;
    public Slug Slug { get; private set; } = null!;
    public string? Icon { get; private set; }
    public int SortOrder { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    private Category()
    {
    }

    public static Category Create(string name, Slug slug, string? icon = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Category { Name = name, Slug = slug, Icon = icon, SortOrder = 0 };
    }

    public void MarkDeleted(DateTime nowUtc)
    {
        IsDeleted = true;
        DeletedAtUtc = nowUtc;
    }
}

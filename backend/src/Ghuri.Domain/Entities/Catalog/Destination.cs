using Ghuri.Domain.Common;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Entities.Catalog;

/// <summary>Places tours go to - Cox's Bazar, Sylhet, Bali... (blueprint: catalog.Destinations [A][S]).</summary>
public sealed class Destination : AggregateRoot, IAuditable, ISoftDeletable
{
    public short CountryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Slug Slug { get; private set; } = null!;
    public string? Summary { get; private set; }

    /// <summary>FK to ops.FileObjects - not configured yet, see the same note on User.AvatarFileId.</summary>
    public Guid? ImageFileId { get; private set; }

    public bool IsFeatured { get; private set; }
    public int SortOrder { get; private set; }
    public string? SeoTitle { get; private set; }
    public string? SeoDescription { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    private Destination()
    {
    }

    public static Destination Create(short countryId, string name, Slug slug, string? summary = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Destination
        {
            CountryId = countryId,
            Name = name,
            Slug = slug,
            Summary = summary,
            IsFeatured = false,
            SortOrder = 0
        };
    }

    public void MarkDeleted(DateTime nowUtc)
    {
        IsDeleted = true;
        DeletedAtUtc = nowUtc;
    }
}

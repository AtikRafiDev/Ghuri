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

    /// <summary>The cover photo - FK to ops.FileObjects (uploaded through POST /api/v1/files).</summary>
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

    public static Destination Create(
        short countryId, string name, Slug slug, string? summary = null, Guid? imageFileId = null,
        bool isFeatured = false, int sortOrder = 0, string? seoTitle = null, string? seoDescription = null)
    {
        var destination = new Destination();
        destination.Update(countryId, name, slug, summary, imageFileId, isFeatured, sortOrder, seoTitle, seoDescription);
        return destination;
    }

    /// <summary>Replaces every editable field at once - the admin form always sends the whole destination.</summary>
    public void Update(
        short countryId, string name, Slug slug, string? summary, Guid? imageFileId,
        bool isFeatured, int sortOrder, string? seoTitle, string? seoDescription)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(slug);

        CountryId = countryId;
        Name = name.Trim();
        Slug = slug;
        Summary = Clean(summary);
        ImageFileId = imageFileId;
        IsFeatured = isFeatured;
        SortOrder = sortOrder;
        SeoTitle = Clean(seoTitle);
        SeoDescription = Clean(seoDescription);
    }

    public void MarkDeleted(DateTime nowUtc)
    {
        IsDeleted = true;
        DeletedAtUtc = nowUtc;
    }

    /// <summary>"" and "   " from an emptied form field are stored as NULL, not as blank text.</summary>
    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}

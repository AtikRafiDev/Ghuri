using Ghuri.Domain.Common;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Entities.Catalog;

/// <summary>Places tours go to - Cox's Bazar, Sylhet, Bali... (blueprint: catalog.Destinations [A][S]).</summary>
public sealed class Destination : AggregateRoot, IAuditable, ISoftDeletable
{
    /// <summary>Enough for a good gallery; keeps the page light on a phone.</summary>
    public const int MaxImages = 10;

    public short CountryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Slug Slug { get; private set; } = null!;
    public string? Summary { get; private set; }
    public bool IsFeatured { get; private set; }
    public int SortOrder { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    private readonly List<DestinationImage> _images = [];

    /// <summary>The photo gallery, in display order. The first one is the cover.</summary>
    public IReadOnlyList<DestinationImage> Images => _images.OrderBy(i => i.SortOrder).ToList();

    private Destination()
    {
    }

    public static Destination Create(
        short countryId, string name, Slug slug, string? summary = null,
        bool isFeatured = false, int sortOrder = 0)
    {
        var destination = new Destination();
        destination.Update(countryId, name, slug, summary, isFeatured, sortOrder);
        return destination;
    }

    /// <summary>Replaces every editable text/display field at once - the admin form always sends the whole destination.</summary>
    public void Update(
        short countryId, string name, Slug slug, string? summary,
        bool isFeatured, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(slug);

        CountryId = countryId;
        Name = name.Trim();
        Slug = slug;
        Summary = Clean(summary);
        IsFeatured = isFeatured;
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Makes the gallery exactly this list of uploaded files, in this order
    /// (the first = the cover). Photos already in the gallery are kept and
    /// only re-ordered; new ones are added; missing ones are removed.
    /// </summary>
    public void SetImages(IReadOnlyList<Guid> fileIds)
    {
        ArgumentNullException.ThrowIfNull(fileIds);
        if (fileIds.Count > MaxImages)
            throw new ArgumentException($"A destination can have at most {MaxImages} photos.", nameof(fileIds));
        if (fileIds.Distinct().Count() != fileIds.Count)
            throw new ArgumentException("The same photo is listed twice.", nameof(fileIds));

        _images.RemoveAll(image => !fileIds.Contains(image.FileId));

        for (var position = 0; position < fileIds.Count; position++)
        {
            var existing = _images.Find(image => image.FileId == fileIds[position]);
            if (existing is null)
                _images.Add(DestinationImage.Create(Id, fileIds[position], position));
            else
                existing.MoveTo(position);
        }
    }

    public void MarkDeleted(DateTime nowUtc)
    {
        IsDeleted = true;
        DeletedAtUtc = nowUtc;
    }

    /// <summary>"" and "   " from an emptied form field are stored as NULL, not as blank text.</summary>
    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}

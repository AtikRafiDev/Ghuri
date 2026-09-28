using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Entities.Catalog;

/// <summary>
/// The product being sold (blueprint: catalog.TourPackages [A][S]). This
/// is the aggregate root for its own gallery images, itinerary days and
/// add-ons - they're only ever reached through this class, never loaded
/// or saved on their own.
/// </summary>
/// <remarks>
/// Publish/Archive and their validation rules (needs a cover image,
/// itinerary days must equal DurationDays, at least one open departure)
/// belong to Day 4's PublishPackage command, built alongside the admin
/// form that actually needs those checks.
/// </remarks>
public sealed class TourPackage : AggregateRoot, IAuditable, ISoftDeletable
{
    /// <summary>Human-readable code like PKG1001, generated from a SQL SEQUENCE (see AppDbContext).</summary>
    public string PackageCode { get; private set; } = string.Empty;

    public Guid DestinationId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public Slug Slug { get; private set; } = null!;
    public string Summary { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public byte DurationDays { get; private set; }
    public byte DurationNights { get; private set; }
    public TourType TourType { get; private set; }

    /// <summary>Free-text bullet points, e.g. "Hotel stay", "Breakfast" - stored as JSON, mapped in TourPackageConfiguration.</summary>
    public List<string> Inclusions { get; private set; } = [];
    public List<string> Exclusions { get; private set; } = [];

    public string? TermsAndPolicy { get; private set; }
    public byte? MinAge { get; private set; }

    /// <summary>Cached lowest adult price across this package's open departures - kept in sync by Day 8/12 feature work, not by this class.</summary>
    public decimal PriceFrom { get; private set; }

    public string Currency { get; private set; } = "BDT";
    public PackageStatus Status { get; private set; }
    public bool IsFeatured { get; private set; }
    public decimal AvgRating { get; private set; }
    public int ReviewCount { get; private set; }
    public string? SeoTitle { get; private set; }
    public string? SeoDescription { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    private readonly List<PackageImage> _images = [];
    public IReadOnlyList<PackageImage> Images => _images.AsReadOnly();

    private readonly List<ItineraryDay> _itineraryDays = [];
    public IReadOnlyList<ItineraryDay> ItineraryDays => _itineraryDays.AsReadOnly();

    private readonly List<PackageAddOn> _addOns = [];
    public IReadOnlyList<PackageAddOn> AddOns => _addOns.AsReadOnly();

    private TourPackage()
    {
    }

    public static TourPackage Create(
        string packageCode, Guid destinationId, string title, Slug slug, string summary,
        byte durationDays, byte durationNights, TourType tourType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);
        if (durationDays is < 1 or > 60)
            throw new ArgumentOutOfRangeException(nameof(durationDays), "Duration must be between 1 and 60 days.");

        return new TourPackage
        {
            PackageCode = packageCode,
            DestinationId = destinationId,
            Title = title,
            Slug = slug,
            Summary = summary,
            DurationDays = durationDays,
            DurationNights = durationNights,
            TourType = tourType,
            Currency = "BDT",
            Status = PackageStatus.Draft,
            IsFeatured = false,
            AvgRating = 0,
            ReviewCount = 0,
            PriceFrom = 0
        };
    }

    // Composition helpers. Each constructs its child via the child's own
    // "internal" factory, using THIS package's Id (already assigned by
    // BaseEntity by the time these run) - callers never construct a
    // PackageImage/ItineraryDay/PackageAddOn themselves.
    public PackageImage AddImage(Guid fileId, int sortOrder, bool isCover, string? caption = null)
    {
        var image = PackageImage.Create(Id, fileId, sortOrder, isCover, caption);
        _images.Add(image);
        return image;
    }

    public ItineraryDay AddItineraryDay(byte dayNo, string title, string description, string? meals = null, string? accommodation = null)
    {
        var day = ItineraryDay.Create(Id, dayNo, title, description, meals, accommodation);
        _itineraryDays.Add(day);
        return day;
    }

    public PackageAddOn AddAddOn(string name, decimal price, PricingUnit pricingUnit)
    {
        var addOn = PackageAddOn.Create(Id, name, price, pricingUnit);
        _addOns.Add(addOn);
        return addOn;
    }

    public void MarkDeleted(DateTime nowUtc)
    {
        IsDeleted = true;
        DeletedAtUtc = nowUtc;
    }
}

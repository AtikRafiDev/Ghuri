using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Exceptions;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Entities.Catalog;

/// <summary>
/// The product being sold (blueprint: catalog.TourPackages [A][S]). This
/// is the aggregate root for its own gallery images, itinerary days,
/// category links and add-ons - they're only ever reached through this
/// class, never loaded or saved on their own.
/// </summary>
/// <remarks>
/// Life cycle: Draft → Published ⇄ Archived. A published package must
/// always stay publishable - any change that would break a publish rule
/// (removing every photo, a fixed itinerary that no longer matches the
/// duration...) throws, so the public site never shows a broken package.
/// </remarks>
public sealed class TourPackage : AggregateRoot, IAuditable, ISoftDeletable
{
    /// <summary>Enough for hotel, room, food and sight photos; keeps the page light on a phone.</summary>
    public const int MaxImages = 15;

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

    // Pricing - copied from a PackagePricing (see ChangePricing). The five
    // flexible-stay fields are null for fixed-departure packages.
    public PricingMode PricingMode { get; private set; }
    public byte? MinNights { get; private set; }
    public byte? MaxNights { get; private set; }
    public decimal? BasePrice { get; private set; }
    public decimal? ExtraNightPrice { get; private set; }
    public byte? MinLeadDays { get; private set; }

    /// <summary>Free-text bullet points, e.g. "Hotel stay", "Breakfast" - stored as JSON, mapped in TourPackageConfiguration.</summary>
    public List<string> Inclusions { get; private set; } = [];
    public List<string> Exclusions { get; private set; } = [];

    public string? TermsAndPolicy { get; private set; }
    public byte? MinAge { get; private set; }

    /// <summary>
    /// The "from ৳…" price on cards, per adult. Flexible: the BasePrice.
    /// Fixed: the lowest adult price of the open departures - kept in sync
    /// by the Day 5 departure commands, 0 until then.
    /// </summary>
    public decimal PriceFrom { get; private set; }

    public string Currency { get; private set; } = "BDT";
    public PackageStatus Status { get; private set; }
    public bool IsFeatured { get; private set; }
    public decimal AvgRating { get; private set; }
    public int ReviewCount { get; private set; }
    public string? SeoTitle { get; private set; }
    public string? SeoDescription { get; private set; }

    /// <summary>When the package FIRST went live - re-publishing after an archive keeps the original date.</summary>
    public DateTime? PublishedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    private readonly List<PackageImage> _images = [];

    /// <summary>The photo gallery, in display order. The first one is the cover.</summary>
    public IReadOnlyList<PackageImage> Images => _images.OrderBy(i => i.SortOrder).ToList();

    private readonly List<ItineraryDay> _itineraryDays = [];
    public IReadOnlyList<ItineraryDay> ItineraryDays => _itineraryDays.OrderBy(d => d.DayNo).ToList();

    private readonly List<PackageCategory> _categories = [];
    public IReadOnlyList<PackageCategory> Categories => _categories.AsReadOnly();

    private readonly List<PackageAddOn> _addOns = [];
    public IReadOnlyList<PackageAddOn> AddOns => _addOns.AsReadOnly();

    private TourPackage()
    {
    }

    /// <summary>A new package always starts as a Draft - nobody sees it until Publish.</summary>
    public static TourPackage Create(string packageCode, TourPackageDetails details, PackagePricing pricing)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageCode);

        var package = new TourPackage
        {
            PackageCode = packageCode,
            Currency = "BDT",
            Status = PackageStatus.Draft,
            AvgRating = 0,
            ReviewCount = 0,
            PriceFrom = 0
        };
        package.Update(details);
        package.ChangePricing(pricing);
        return package;
    }

    /// <summary>Replaces every descriptive field at once - the admin form always sends the whole package.</summary>
    public void Update(TourPackageDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        ArgumentException.ThrowIfNullOrWhiteSpace(details.Title);
        ArgumentNullException.ThrowIfNull(details.Slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(details.Summary);
        if (details.DestinationId == Guid.Empty)
            throw new ArgumentException("A package needs a destination.", nameof(details));

        DestinationId = details.DestinationId;
        Title = details.Title.Trim();
        Slug = details.Slug;
        Summary = details.Summary.Trim();
        Description = Clean(details.Description);
        TourType = details.TourType;
        Inclusions = CleanList(details.Inclusions);
        Exclusions = CleanList(details.Exclusions);
        TermsAndPolicy = Clean(details.TermsAndPolicy);
        MinAge = details.MinAge;
        IsFeatured = details.IsFeatured;
        SeoTitle = Clean(details.SeoTitle);
        SeoDescription = Clean(details.SeoDescription);
    }

    /// <summary>
    /// Sets how the package is sold and how long it lasts. The mode
    /// (fixed ⇄ flexible) can only change while the package is a Draft:
    /// once it has been live, customers may have booked it the old way.
    /// </summary>
    public void ChangePricing(PackagePricing pricing)
    {
        ArgumentNullException.ThrowIfNull(pricing);
        if (Status != PackageStatus.Draft && pricing.Mode != PricingMode)
            throw new DomainException(
                "package_pricing_mode_locked",
                "The pricing mode can only be changed while the package is a draft.");

        PricingMode = pricing.Mode;
        DurationDays = pricing.DurationDays;
        DurationNights = pricing.DurationNights;
        MinNights = pricing.MinNights;
        MaxNights = pricing.MaxNights;
        BasePrice = pricing.BasePrice;
        ExtraNightPrice = pricing.ExtraNightPrice;
        MinLeadDays = pricing.MinLeadDays;

        if (pricing.Mode == PricingMode.FlexibleStay)
            PriceFrom = pricing.BasePrice!.Value;
        else if (Status == PackageStatus.Draft)
            PriceFrom = 0; // a draft that was flexible until now has no departures yet

        EnsureStillPublishable();
    }

    /// <summary>Makes the package's categories exactly this set. Duplicates are ignored.</summary>
    public void SetCategories(IEnumerable<Guid> categoryIds)
    {
        ArgumentNullException.ThrowIfNull(categoryIds);
        var wanted = categoryIds.ToHashSet();

        _categories.RemoveAll(link => !wanted.Contains(link.CategoryId));
        foreach (var categoryId in wanted.Where(id => _categories.TrueForAll(link => link.CategoryId != id)))
            _categories.Add(PackageCategory.Create(Id, categoryId));
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
            throw new ArgumentException($"A package can have at most {MaxImages} photos.", nameof(fileIds));
        if (fileIds.Distinct().Count() != fileIds.Count)
            throw new ArgumentException("The same photo is listed twice.", nameof(fileIds));

        _images.RemoveAll(image => !fileIds.Contains(image.FileId));

        for (var position = 0; position < fileIds.Count; position++)
        {
            var existing = _images.Find(image => image.FileId == fileIds[position]);
            if (existing is null)
                _images.Add(PackageImage.Create(Id, fileIds[position], position));
            else
                existing.MoveTo(position);
        }

        EnsureStillPublishable();
    }

    /// <summary>
    /// Makes the itinerary exactly these days: the first is Day 1, the
    /// second Day 2, and so on. Rows for day numbers that still exist are
    /// updated in place (never deleted and re-added), so the database's
    /// "one row per day number" unique index can't trip over itself.
    /// </summary>
    public void SetItinerary(IReadOnlyList<ItineraryDayDetails> days)
    {
        ArgumentNullException.ThrowIfNull(days);
        if (days.Count > PackagePricing.MaxDays)
            throw new ArgumentException($"An itinerary can have at most {PackagePricing.MaxDays} days.", nameof(days));

        _itineraryDays.RemoveAll(day => day.DayNo > days.Count);

        for (var index = 0; index < days.Count; index++)
        {
            var dayNo = (byte)(index + 1);
            var existing = _itineraryDays.Find(day => day.DayNo == dayNo);
            if (existing is null)
                _itineraryDays.Add(ItineraryDay.Create(Id, dayNo, days[index]));
            else
                existing.Update(days[index]);
        }

        EnsureStillPublishable();
    }

    public PackageAddOn AddAddOn(string name, decimal price, PricingUnit pricingUnit)
    {
        var addOn = PackageAddOn.Create(Id, name, price, pricingUnit);
        _addOns.Add(addOn);
        return addOn;
    }

    /// <summary>
    /// Everything that stops this package going live, as messages for the
    /// admin. Empty = ready to publish. The PublishPackage handler shows
    /// the whole list at once instead of one error at a time.
    /// </summary>
    /// <remarks>
    /// Flexible-stay nights and prices aren't checked here: PackagePricing
    /// can't be created without them. Day 5 adds "a fixed package needs at
    /// least one open departure" - departures don't exist yet on Day 4.
    /// </remarks>
    public IReadOnlyList<string> GetPublishProblems()
    {
        var problems = new List<string>();

        if (_images.Count == 0)
            problems.Add("Add at least one photo. The first photo is the cover.");

        var dayCount = _itineraryDays.Count;
        if (dayCount == 0)
            problems.Add("Add the day-by-day itinerary.");
        else if (PricingMode == PricingMode.FixedDepartures && dayCount != DurationDays)
            problems.Add($"The itinerary has {dayCount} days, but the package lasts {DurationDays} days.");
        else if (PricingMode == PricingMode.FlexibleStay && dayCount > MaxNights + 1)
            problems.Add($"The itinerary has {dayCount} days, but the longest stay is {MaxNights + 1} days.");

        return problems;
    }

    /// <summary>Puts the package on the public site. Works from Draft and from Archived.</summary>
    public void Publish(DateTime nowUtc)
    {
        if (Status == PackageStatus.Published)
            throw new DomainException("package_already_published", "This package is already published.");

        var problems = GetPublishProblems();
        if (problems.Count > 0)
            throw new DomainException("package_not_publishable", string.Join(" ", problems));

        Status = PackageStatus.Published;
        PublishedAtUtc ??= nowUtc;
    }

    /// <summary>Takes the package off the public site but keeps it (and its bookings) for the record.</summary>
    public void Archive()
    {
        if (Status == PackageStatus.Archived)
            throw new DomainException("package_already_archived", "This package is already archived.");

        Status = PackageStatus.Archived;
    }

    public void MarkDeleted(DateTime nowUtc)
    {
        IsDeleted = true;
        DeletedAtUtc = nowUtc;
    }

    /// <summary>
    /// Called after every change to a published package. It throws, so the
    /// request fails and nothing is saved - the admin must archive the
    /// package first to make a change that breaks a publish rule.
    /// </summary>
    private void EnsureStillPublishable()
    {
        if (Status != PackageStatus.Published)
            return;

        var problems = GetPublishProblems();
        if (problems.Count > 0)
            throw new DomainException(
                "package_must_stay_publishable",
                "A published package must stay complete. " + string.Join(" ", problems));
    }

    /// <summary>"" and "   " from an emptied form field are stored as NULL, not as blank text.</summary>
    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    /// <summary>Trims each bullet point and drops empty ones (a blank row left in the form).</summary>
    private static List<string> CleanList(IReadOnlyList<string>? items) =>
        items?.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToList() ?? [];
}

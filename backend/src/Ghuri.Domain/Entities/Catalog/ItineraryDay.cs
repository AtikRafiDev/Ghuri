using Ghuri.Domain.Common;

namespace Ghuri.Domain.Entities.Catalog;

/// <summary>One day of a package's day-by-day plan. Owned by TourPackage - see PackageImage's remarks.</summary>
public sealed class ItineraryDay : BaseEntity
{
    public Guid PackageId { get; private set; }
    public byte DayNo { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    /// <summary>Meal flags, e.g. "B,L,D" for breakfast/lunch/dinner included.</summary>
    public string? Meals { get; private set; }

    public string? Accommodation { get; private set; }

    private ItineraryDay()
    {
    }

    internal static ItineraryDay Create(
        Guid packageId, byte dayNo, string title, string description, string? meals = null, string? accommodation = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new ItineraryDay
        {
            PackageId = packageId,
            DayNo = dayNo,
            Title = title,
            Description = description,
            Meals = meals,
            Accommodation = accommodation
        };
    }
}

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

    internal static ItineraryDay Create(Guid packageId, byte dayNo, ItineraryDayDetails details)
    {
        var day = new ItineraryDay { PackageId = packageId, DayNo = dayNo };
        day.Update(details);
        return day;
    }

    /// <summary>Replaces the day's text. DayNo never changes - TourPackage.SetItinerary keeps each row on its own day.</summary>
    internal void Update(ItineraryDayDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        ArgumentException.ThrowIfNullOrWhiteSpace(details.Title);
        ArgumentException.ThrowIfNullOrWhiteSpace(details.Description);

        Title = details.Title.Trim();
        Description = details.Description.Trim();
        Meals = string.IsNullOrWhiteSpace(details.Meals) ? null : details.Meals.Trim();
        Accommodation = string.IsNullOrWhiteSpace(details.Accommodation) ? null : details.Accommodation.Trim();
    }
}

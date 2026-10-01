namespace Ghuri.Domain.Entities.Catalog;

/// <summary>
/// What the admin types for one itinerary day. The day number isn't here -
/// it comes from the day's position in the list given to TourPackage.SetItinerary.
/// </summary>
public sealed record ItineraryDayDetails(
    string Title,
    string Description,
    string? Meals = null,
    string? Accommodation = null);

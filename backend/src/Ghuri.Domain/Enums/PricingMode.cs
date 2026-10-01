namespace Ghuri.Domain.Enums;

/// <summary>Maps to catalog.TourPackages.PricingMode (TINYINT). How a package is sold.</summary>
public enum PricingMode : byte
{
    /// <summary>Set dates with their own prices and seats (catalog.Departures).</summary>
    FixedDepartures = 1,

    /// <summary>The customer picks the start date and how many nights; priced per night.</summary>
    FlexibleStay = 2
}

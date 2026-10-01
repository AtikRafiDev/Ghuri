namespace Ghuri.Domain.Enums;

/// <summary>The answer of TourPackage.CheckFlexibleStay - not stored anywhere.</summary>
public enum FlexibleStayBookability
{
    Bookable = 0,

    /// <summary>Fewer than MinNights or more than MaxNights.</summary>
    NightsOutOfRange = 1,

    /// <summary>Starts sooner than MinLeadDays from today - no time to confirm the hotel.</summary>
    TooSoon = 2
}

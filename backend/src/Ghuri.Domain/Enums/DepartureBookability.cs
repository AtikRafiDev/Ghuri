namespace Ghuri.Domain.Enums;

/// <summary>The answer of Departure.CheckBookable - not stored anywhere.</summary>
public enum DepartureBookability
{
    Bookable = 0,

    /// <summary>Closed, cancelled or completed.</summary>
    NotOpen = 1,

    /// <summary>Past the last booking date (StartDate - BookingCutoffDays), or already departed.</summary>
    BookingClosed = 2,

    NotEnoughSeats = 3
}

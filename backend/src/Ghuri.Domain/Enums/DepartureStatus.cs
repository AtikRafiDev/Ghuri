namespace Ghuri.Domain.Enums;

/// <summary>Maps to catalog.Departures.Status (TINYINT).</summary>
public enum DepartureStatus : byte
{
    Open = 1,
    Closed = 2,
    Cancelled = 3,
    Completed = 4
}

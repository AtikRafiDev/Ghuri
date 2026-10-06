namespace Ghuri.Domain.Enums;

/// <summary>Maps to booking.CustomTripLegs.TransferToNext (TINYINT) - how to get from one leg to the next. The last leg has None.</summary>
public enum TransferMode : byte
{
    None = 1,
    Bus = 2,
    Train = 3,
    Air = 4,
    PrivateCar = 5,
    Launch = 6
}

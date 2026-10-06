namespace Ghuri.Domain.Enums;

/// <summary>Maps to booking.CustomTripQuoteLines.Category (TINYINT) - groups a quote's price lines.</summary>
public enum QuoteLineCategory : byte
{
    Hotel = 1,
    Transport = 2,
    Meals = 3,
    Guide = 4,
    Activities = 5,
    Other = 6
}

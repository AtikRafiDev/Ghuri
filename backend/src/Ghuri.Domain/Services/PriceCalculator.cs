using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Services;

/// <summary>One line of a price breakdown, e.g. "Adult" ৳12,000 × 2 = ৳24,000.</summary>
public sealed record PriceLine(string Label, decimal UnitPrice, int Quantity)
{
    public decimal Amount => UnitPrice * Quantity;
}

/// <summary>Every line of a price, and their total. The checkout and the voucher show the lines as they are.</summary>
public sealed record PriceBreakdown(IReadOnlyList<PriceLine> Lines, string Currency)
{
    public decimal Total => Lines.Sum(line => line.Amount);
}

/// <summary>
/// What a trip costs - the ONE place prices are worked out (17-day plan,
/// Day 6). The quote (Day 6), the booking (Day 8) and the payment (Day 9)
/// all use it, so they can never disagree, and the server never trusts a
/// price sent by the browser.
/// </summary>
/// <remarks>
/// A "domain service": pure rules that need more than one object
/// (a package AND a departure AND the travellers), so they don't belong
/// inside any single entity. No database, no clock - easy to test.
/// </remarks>
public static class PriceCalculator
{
    /// <summary>
    /// Fixed departure: each traveller type pays the departure's own price;
    /// a single room adds the single supplement per room.
    /// </summary>
    public static PriceBreakdown ForDeparture(TourPackage package, Departure departure, Travellers travellers, int singleRooms)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(departure);
        ArgumentNullException.ThrowIfNull(travellers);
        if (departure.PackageId != package.Id)
            throw new ArgumentException("The departure belongs to another package.", nameof(departure));
        if (singleRooms < 0 || singleRooms > travellers.Adults)
            throw new ArgumentOutOfRangeException(nameof(singleRooms), "Single rooms: from 0 up to the number of adults.");
        if (singleRooms > 0 && departure.SingleSupplement is null)
            throw new ArgumentException("This departure doesn't offer single rooms.", nameof(singleRooms));

        var lines = new List<PriceLine> { new("Adult", departure.AdultPrice, travellers.Adults) };
        if (travellers.Children > 0)
            lines.Add(new PriceLine("Child", departure.ChildPrice, travellers.Children));
        if (travellers.Infants > 0)
            lines.Add(new PriceLine("Infant", departure.InfantPrice, travellers.Infants));
        if (singleRooms > 0)
            lines.Add(new PriceLine("Single room supplement", departure.SingleSupplement!.Value, singleRooms));

        return new PriceBreakdown(lines, package.Currency);
    }

    /// <summary>
    /// Flexible stay: per person, BasePrice covers MinNights and each night
    /// after that adds ExtraNightPrice. Children pay the adult rate (until
    /// the client decides on a child rate); infants are free.
    /// </summary>
    public static PriceBreakdown ForFlexibleStay(TourPackage package, int nights, Travellers travellers)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(travellers);
        if (package.PricingMode != PricingMode.FlexibleStay)
            throw new ArgumentException("Not a flexible-stay package.", nameof(package));
        if (nights < package.MinNights || nights > package.MaxNights)
            throw new ArgumentOutOfRangeException(nameof(nights), $"Choose {package.MinNights} to {package.MaxNights} nights.");

        var perPerson = PerPersonForNights(package, nights);
        var label = $"{nights} nights";

        var lines = new List<PriceLine> { new($"Adult · {label}", perPerson, travellers.Adults) };
        if (travellers.Children > 0)
            lines.Add(new PriceLine($"Child · {label}", perPerson, travellers.Children));
        if (travellers.Infants > 0)
            lines.Add(new PriceLine("Infant", 0, travellers.Infants));

        return new PriceBreakdown(lines, package.Currency);
    }

    /// <summary>One person's price for a flexible stay of this many nights (already checked above).</summary>
    private static decimal PerPersonForNights(TourPackage package, int nights) =>
        package.BasePrice!.Value + (nights - package.MinNights!.Value) * package.ExtraNightPrice!.Value;
}

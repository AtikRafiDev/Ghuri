using Ghuri.Domain.Enums;

namespace Ghuri.Domain.ValueObjects;

/// <summary>
/// How a package is sold and how long it lasts - always a valid
/// combination, because the only way to make one is through the two
/// factories below. TourPackage copies these values into its own columns.
/// </summary>
/// <remarks>
/// Flexible-stay price: BasePrice is per adult and covers MinNights;
/// every night after that adds ExtraNightPrice per adult. Fixed packages
/// get their per-adult prices from each Departure instead, so the Day 6
/// PriceCalculator works the same way for both modes.
/// </remarks>
public sealed record PackagePricing
{
    /// <summary>Same limit as the CK_TourPackages_DurationDays check in the database.</summary>
    public const byte MaxDays = 60;

    public PricingMode Mode { get; }
    public byte DurationDays { get; }
    public byte DurationNights { get; }

    // Flexible stay only - null for fixed departures.
    public byte? MinNights { get; }
    public byte? MaxNights { get; }
    public decimal? BasePrice { get; }
    public decimal? ExtraNightPrice { get; }

    /// <summary>How many days ahead the stay must be booked, so staff have time to reserve the hotel.</summary>
    public byte? MinLeadDays { get; }

    private PackagePricing(
        PricingMode mode, byte durationDays, byte durationNights,
        byte? minNights = null, byte? maxNights = null,
        decimal? basePrice = null, decimal? extraNightPrice = null, byte? minLeadDays = null)
    {
        Mode = mode;
        DurationDays = durationDays;
        DurationNights = durationNights;
        MinNights = minNights;
        MaxNights = maxNights;
        BasePrice = basePrice;
        ExtraNightPrice = extraNightPrice;
        MinLeadDays = minLeadDays;
    }

    /// <summary>A package sold on set dates, e.g. 3 days / 2 nights.</summary>
    public static PackagePricing FixedDepartures(byte durationDays, byte durationNights)
    {
        if (durationDays is < 1 or > MaxDays)
            throw new ArgumentOutOfRangeException(nameof(durationDays), $"Duration must be between 1 and {MaxDays} days.");
        if (durationNights > MaxDays)
            throw new ArgumentOutOfRangeException(nameof(durationNights), $"Nights cannot be more than {MaxDays}.");

        return new PackagePricing(PricingMode.FixedDepartures, durationDays, durationNights);
    }

    /// <summary>
    /// A stay of MinNights..MaxNights nights from any start date. The
    /// package's duration becomes the shortest stay (MinNights + 1 days),
    /// which is what search cards show as "from".
    /// </summary>
    public static PackagePricing FlexibleStay(
        byte minNights, byte maxNights, decimal basePrice, decimal extraNightPrice, byte minLeadDays)
    {
        if (minNights < 1)
            throw new ArgumentOutOfRangeException(nameof(minNights), "A stay must be at least 1 night.");
        if (maxNights < minNights)
            throw new ArgumentOutOfRangeException(nameof(maxNights), "Maximum nights cannot be less than minimum nights.");
        // The longest stay (MaxNights + 1 days) must still fit the 60-day limit.
        if (maxNights >= MaxDays)
            throw new ArgumentOutOfRangeException(nameof(maxNights), $"A stay cannot be longer than {MaxDays - 1} nights.");
        if (basePrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(basePrice), "Base price must be more than zero.");
        if (extraNightPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(extraNightPrice), "Extra-night price cannot be negative.");

        return new PackagePricing(
            PricingMode.FlexibleStay, (byte)(minNights + 1), minNights,
            minNights, maxNights, basePrice, extraNightPrice, minLeadDays);
    }
}

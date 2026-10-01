namespace Ghuri.Domain.ValueObjects;

/// <summary>
/// Who is coming on one booking. Always valid: at least one adult, no
/// negative counts, at most MaxPerBooking people, and every infant has an
/// adult to sit with.
/// </summary>
/// <remarks>
/// Infants (under 2) travel on an adult's lap, so they don't take a seat:
/// <see cref="Seats"/> counts adults and children only.
/// </remarks>
public sealed record Travellers
{
    /// <summary>A family or a small group - bigger groups are a custom trip, quoted by staff.</summary>
    public const int MaxPerBooking = 20;

    public int Adults { get; }
    public int Children { get; }
    public int Infants { get; }

    public int People => Adults + Children + Infants;

    /// <summary>Seats taken on a departure: infants sit on a lap.</summary>
    public int Seats => Adults + Children;

    private Travellers(int adults, int children, int infants)
    {
        Adults = adults;
        Children = children;
        Infants = infants;
    }

    public static Travellers Create(int adults, int children = 0, int infants = 0)
    {
        if (adults < 1)
            throw new ArgumentOutOfRangeException(nameof(adults), "At least one adult must travel.");
        if (children < 0 || infants < 0)
            throw new ArgumentOutOfRangeException(nameof(children), "Counts cannot be negative.");
        if (infants > adults)
            throw new ArgumentOutOfRangeException(nameof(infants), "Each infant needs an adult to travel with.");
        if (adults + children + infants > MaxPerBooking)
            throw new ArgumentOutOfRangeException(nameof(adults), $"At most {MaxPerBooking} people per booking.");

        return new Travellers(adults, children, infants);
    }
}

namespace Ghuri.Domain.ValueObjects;

/// <summary>
/// An amount of money in a specific currency, with arithmetic that
/// refuses to silently mix currencies.
/// </summary>
/// <remarks>
/// NOT mapped to any database column (yet). The blueprint's actual tables
/// store a plain decimal amount column and often ONE shared Currency
/// column per ROW covering several amount columns (e.g. booking.Bookings
/// has one Currency column shared by SubTotal, AddOnTotal, TotalAmount...)
/// rather than a currency paired with every single amount. Forcing this
/// type onto each column would mean adding currency columns the blueprint
/// never asked for. So entities map their amount/currency columns as
/// plain decimal/string for schema accuracy, and this type is used inside
/// domain SERVICES that do calculations (PriceCalculator etc.), built
/// later alongside the booking feature.
/// </remarks>
public readonly struct Money : IEquatable<Money>, IComparable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "BDT")
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter code, e.g. BDT.", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency = "BDT") => new(0m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        // "Rounding: half away from zero" - blueprint section 5.1.
        return new Money(decimal.Round(Amount + other.Amount, 2, MidpointRounding.AwayFromZero), Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(decimal.Round(Amount - other.Amount, 2, MidpointRounding.AwayFromZero), Currency);
    }

    public Money Multiply(int factor) =>
        new(decimal.Round(Amount * factor, 2, MidpointRounding.AwayFromZero), Currency);

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot combine {Currency} with {other.Currency}.");
    }

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);
    public static Money operator *(Money money, int factor) => money.Multiply(factor);

    public bool Equals(Money other) => Amount == other.Amount && Currency == other.Currency;
    public override bool Equals(object? obj) => obj is Money other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public int CompareTo(Money other) =>
        Currency == other.Currency
            ? Amount.CompareTo(other.Amount)
            : throw new InvalidOperationException($"Cannot compare {Currency} with {other.Currency}.");

    public override string ToString() => $"{Amount:N2} {Currency}";
}

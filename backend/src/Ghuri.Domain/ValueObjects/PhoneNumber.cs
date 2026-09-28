namespace Ghuri.Domain.ValueObjects;

/// <summary>
/// A Bangladeshi mobile number, always stored normalized as
/// +8801XXXXXXXXX (blueprint section 5.1). Accepts the common ways a
/// person might type it in - 01XXXXXXXXX, 8801XXXXXXXXX, +8801XXXXXXXXX,
/// with or without spaces/dashes - and rejects anything else, so every
/// other part of the app can trust the format without re-checking it.
/// </summary>
public sealed class PhoneNumber : IEquatable<PhoneNumber>
{
    public string Value { get; }

    private PhoneNumber(string value) => Value = value;

    public static bool IsValid(string? input) => TryCreate(input, out _);

    public static PhoneNumber Create(string input) =>
        TryCreate(input, out var phone)
            ? phone!
            : throw new ArgumentException($"'{input}' is not a valid Bangladeshi mobile number.", nameof(input));

    public static bool TryCreate(string? input, out PhoneNumber? phoneNumber)
    {
        phoneNumber = null;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        // People paste numbers formatted all sorts of ways
        // ("+880 171-234-5678") - keep only the digits.
        var digitsOnly = new string(input.Where(char.IsDigit).ToArray());

        // Normalize every accepted shape down to the 11-digit local form
        // "01XXXXXXXXX" first, then re-add the +880 country code.
        string local;
        if (digitsOnly.Length == 13 && digitsOnly.StartsWith("8801", StringComparison.Ordinal))
            local = digitsOnly[2..]; // drop the leading "88", keep "01XXXXXXXXX"
        else if (digitsOnly.Length == 11 && digitsOnly.StartsWith("01", StringComparison.Ordinal))
            local = digitsOnly;
        else
            return false;

        // The 3rd digit is the operator prefix (013 Grameenphone, 014/019
        // Banglalink, 015 Teletalk, 016 Airtel, 017 Grameenphone, 018
        // Robi...) - valid range is 3-9. Anything else isn't a real BD
        // mobile prefix.
        var operatorDigit = local[2];
        if (operatorDigit is < '3' or > '9')
            return false;

        phoneNumber = new PhoneNumber($"+880{local[1..]}");
        return true;
    }

    public bool Equals(PhoneNumber? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => Equals(obj as PhoneNumber);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static implicit operator string(PhoneNumber phone) => phone.Value;
}

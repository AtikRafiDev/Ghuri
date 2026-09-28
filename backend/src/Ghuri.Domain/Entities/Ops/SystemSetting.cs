namespace Ghuri.Domain.Entities.Ops;

/// <summary>
/// A runtime-configurable business setting (blueprint: ops.SystemSettings),
/// e.g. "Booking.HoldMinutes" = "20". Value is always stored as text;
/// ValueType tells the reader how to parse it back (int/decimal/bool/
/// string/json) - see the blueprint's appendix for the seeded defaults.
/// </summary>
public sealed class SystemSetting
{
    /// <summary>The setting's name, e.g. "Booking.HoldMinutes" - the primary key.</summary>
    public string Key { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;
    public string ValueType { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    private SystemSetting()
    {
    }

    public static SystemSetting Create(string key, string value, string valueType, DateTime nowUtc, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(valueType);

        return new SystemSetting
        {
            Key = key,
            Value = value,
            ValueType = valueType,
            Description = description,
            UpdatedAtUtc = nowUtc
        };
    }

    public void Update(string value, DateTime nowUtc, Guid? updatedBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
        UpdatedAtUtc = nowUtc;
        UpdatedBy = updatedBy;
    }
}

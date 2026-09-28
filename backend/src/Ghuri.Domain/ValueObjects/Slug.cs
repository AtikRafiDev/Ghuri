using System.Text;

namespace Ghuri.Domain.ValueObjects;

/// <summary>
/// A URL-friendly identifier like "cox-s-bazar-3-days" - lowercase,
/// hyphenated, no spaces or special characters. Used for readable public
/// URLs (/packages/cox-s-bazar-3-days) instead of exposing a raw Guid.
/// This class only guarantees the FORMAT is valid; uniqueness is enforced
/// by a database unique index, not here.
/// </summary>
public sealed class Slug : IEquatable<Slug>
{
    public string Value { get; }

    private Slug(string value) => Value = value;

    public static Slug Create(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return new Slug(GenerateFrom(text));
    }

    /// <summary>Builds a slug from free text, e.g. "Cox's Bazar!" -> "coxs-bazar".</summary>
    public static string GenerateFrom(string text)
    {
        var lowered = text.Trim().ToLowerInvariant();
        var builder = new StringBuilder(lowered.Length);
        var lastWasHyphen = false;

        foreach (var c in lowered)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen && builder.Length > 0)
            {
                builder.Append('-');
                lastWasHyphen = true;
            }
        }

        return builder.ToString().TrimEnd('-');
    }

    public bool Equals(Slug? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => Equals(obj as Slug);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static implicit operator string(Slug slug) => slug.Value;
}

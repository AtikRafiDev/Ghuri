using System.Text.RegularExpressions;
using FluentValidation;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Application.Features.Catalog;

/// <summary>
/// The URL name (slug) of a destination or category: the one the admin
/// typed, or - if they left it empty - one made from the name.
/// "Cox's Bazar" -> "cox-s-bazar" -> /destinations/cox-s-bazar.
/// </summary>
public static partial class CatalogSlug
{
    public static Slug Build(string? slug, string name) => Slug.Create(Source(slug, name));

    /// <summary>
    /// Validator rule: the resulting slug must be plain English letters,
    /// digits and hyphens. The column is ASCII-only (VARCHAR), so a Bangla
    /// name like "কক্সবাজার" would otherwise be saved as "?????" - the admin
    /// must type an English slug for it instead.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> ValidSlugFor<T>(
        this IRuleBuilder<T, string?> rule, Func<T, string?> name, int maxLength) =>
        rule.Must((item, slug) =>
            {
                var source = Source(slug, name(item));
                if (string.IsNullOrWhiteSpace(source))
                    return true; // the empty name gets its own "required" message
                var generated = Slug.GenerateFrom(source);
                return generated.Length is > 0 && generated.Length <= maxLength && AsciiSlug().IsMatch(generated);
            })
            .WithMessage($"Enter a URL name using English letters, numbers and hyphens (max {maxLength}), e.g. coxs-bazar.");

    private static string Source(string? slug, string? name) => string.IsNullOrWhiteSpace(slug) ? name ?? string.Empty : slug;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex AsciiSlug();
}

namespace Ghuri.Infrastructure.Documents;

/// <summary>
/// Settings from appsettings.json "Agency": who issues the invoice and the
/// voucher - printed at the top of both PDFs. The real details come from
/// the client before launch; the defaults are placeholders.
/// </summary>
internal sealed class AgencyOptions
{
    public const string SectionName = "Agency";

    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
}

namespace Ghuri.Domain.Entities.Catalog;

/// <summary>
/// A country, seeded once and rarely changed (e.g. "Bangladesh", "BD").
/// Matches blueprint table catalog.Countries.
/// </summary>
/// <remarks>
/// Unlike most entities in this system, Country does NOT use a Guid id.
/// The blueprint deliberately gives small, admin-only reference tables a
/// simple SQL Server IDENTITY (auto-increment) integer instead - there is
/// no reason to generate a random-looking Guid for something like "198.
/// Bangladesh". Guids are reserved for entities the customer-facing app
/// creates a lot of (bookings, payments...), where generating the id in
/// C# before saving is genuinely useful.
/// </remarks>
public sealed class Country
{
    public short Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string IsoCode { get; private set; } = string.Empty;

    // EF Core needs a parameterless constructor to build objects when
    // reading rows back from the database. It's private so nobody outside
    // this class can create a "blank" Country by accident - the only way
    // in is the Create factory method below.
    private Country()
    {
    }

    public static Country Create(string name, string isoCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        return new Country
        {
            Name = name,
            IsoCode = isoCode.ToUpperInvariant()
        };
    }
}

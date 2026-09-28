using Ghuri.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations;

/// <summary>
/// Tells EF Core exactly how a Country object maps onto the
/// catalog.Countries table - this is the code-first replacement for
/// hand-writing a CREATE TABLE statement.
/// </summary>
internal sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        // "catalog" here is the SQL Server SCHEMA, not the database name.
        // The blueprint groups every module's tables under its own schema
        // (catalog, booking, payment...) inside one single database.
        builder.ToTable("Countries", schema: "catalog");

        builder.HasKey(c => c.Id);

        // SMALLINT IDENTITY in the blueprint = SQL Server generates the
        // number itself (1, 2, 3...) when a row is inserted. This is the
        // opposite of the Guid pattern we'll use for bigger aggregates,
        // where the C# code picks the id before saving.
        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Name)
            .HasMaxLength(100)
            .IsRequired();

        // Blueprint: "UQ" on Name - two countries can't share a name.
        builder.HasIndex(c => c.Name)
            .IsUnique();

        // Blueprint: CHAR(2) - always exactly 2 letters (ISO 3166-1 alpha-2,
        // e.g. "BD", "IN"). IsFixedLength maps to CHAR instead of VARCHAR
        // (length never varies); IsUnicode(false) maps to CHAR instead of
        // NCHAR - country codes are always plain ASCII, so there's no reason
        // to reserve double the storage for Unicode characters we'll never
        // store here.
        builder.Property(c => c.IsoCode)
            .HasMaxLength(2)
            .IsFixedLength()
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(c => c.IsoCode)
            .IsUnique();
    }
}

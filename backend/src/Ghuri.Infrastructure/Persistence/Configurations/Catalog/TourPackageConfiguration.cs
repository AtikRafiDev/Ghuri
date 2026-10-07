using System.Text.Json;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class TourPackageConfiguration : IEntityTypeConfiguration<TourPackage>
{
    public void Configure(EntityTypeBuilder<TourPackage> builder)
    {
        builder.ToTable("TourPackages", schema: "catalog");

        builder.HasKey(p => p.Id);
        builder.HasAuditColumns();
        builder.HasSoftDeleteFilter();

        builder.Property(p => p.PackageCode).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.HasIndex(p => p.PackageCode).IsUnique();

        builder.Property(p => p.DestinationId).IsRequired();
        builder.HasOne<Destination>()
            .WithMany()
            .HasForeignKey(p => p.DestinationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.DestinationId);

        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();

        builder.Property(p => p.Slug)
            .HasConversion(s => s.Value, v => Slug.Create(v))
            .HasMaxLength(220)
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(p => p.Slug).IsUnique();

        builder.Property(p => p.Summary).HasMaxLength(1000).IsRequired();
        builder.Property(p => p.Description); // NVARCHAR(MAX), nullable

        builder.Property(p => p.DurationDays).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_TourPackages_DurationDays", "[DurationDays] BETWEEN 1 AND 60"));
        builder.Property(p => p.DurationNights).IsRequired();

        builder.Property(p => p.TourType).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_TourPackages_TourType", "[TourType] IN (1,2,3)"));

        builder.Property(p => p.PricingMode).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_TourPackages_PricingMode", "[PricingMode] IN (1,2)"));

        // Flexible-stay fields: nullable, because fixed packages don't use them.
        builder.Property(p => p.MinNights);
        builder.Property(p => p.MaxNights);
        builder.Property(p => p.BasePrice).HasColumnType("decimal(18,2)");
        builder.Property(p => p.ExtraNightPrice).HasColumnType("decimal(18,2)");
        builder.Property(p => p.MinLeadDays);

        // The same rules as PackagePricing, as a last line of defence in
        // the database: fixed = all five NULL; flexible = all five filled
        // and sensible. The "IS NOT NULL" checks are not redundant - in SQL,
        // "NULL >= 1" is UNKNOWN (not FALSE), and a CHECK constraint lets
        // UNKNOWN through. Without them a flexible row with NULL prices
        // would be accepted.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_TourPackages_FlexibleStay",
            "([PricingMode] = 1 AND [MinNights] IS NULL AND [MaxNights] IS NULL AND [BasePrice] IS NULL "
            + "AND [ExtraNightPrice] IS NULL AND [MinLeadDays] IS NULL) "
            + "OR ([PricingMode] = 2 AND [MinNights] IS NOT NULL AND [MaxNights] IS NOT NULL AND [BasePrice] IS NOT NULL "
            + "AND [ExtraNightPrice] IS NOT NULL AND [MinLeadDays] IS NOT NULL "
            + "AND [MinNights] >= 1 AND [MaxNights] >= [MinNights] AND [BasePrice] > 0 AND [ExtraNightPrice] >= 0)"));

        ConfigureJsonStringList(builder.Property(p => p.Inclusions));
        ConfigureJsonStringList(builder.Property(p => p.Exclusions));

        builder.Property(p => p.TermsAndPolicy); // NVARCHAR(MAX), nullable
        builder.Property(p => p.MinAge);

        builder.Property(p => p.PriceFrom).HasColumnType("decimal(18,2)").IsRequired();
        builder.HasIndex(p => p.PriceFrom);

        builder.Property(p => p.Currency).HasMaxLength(3).IsFixedLength().IsUnicode(false).IsRequired();

        builder.Property(p => p.Status).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_TourPackages_Status", "[Status] IN (1,2,3)"));
        builder.HasIndex(p => p.Status);

        builder.Property(p => p.IsFeatured).IsRequired();
        builder.HasIndex(p => p.IsFeatured);

        builder.Property(p => p.AvgRating).HasColumnType("decimal(3,2)").IsRequired();
        builder.Property(p => p.ReviewCount).IsRequired();

        builder.Property(p => p.PublishedAtUtc);

        // Optimistic concurrency: SQL Server auto-updates this on every
        // write. EF Core includes it in the WHERE clause of UPDATE/DELETE
        // statements, so a second person saving stale data gets a
        // DbUpdateConcurrencyException instead of silently overwriting
        // someone else's change.
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        // The three owned collections. WithOne() with no argument means
        // "there's no navigation property going back from the child to
        // TourPackage" - PackageImage doesn't hold a reference to its
        // parent, only its PackageId foreign key.
        builder.HasMany(p => p.Images)
            .WithOne()
            .HasForeignKey(pi => pi.PackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.ItineraryDays)
            .WithOne()
            .HasForeignKey(d => d.PackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.AddOns)
            .WithOne()
            .HasForeignKey(a => a.PackageId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// Stores a List&lt;string&gt; as one NVARCHAR(MAX) JSON column (the
    /// blueprint's "JSON array of strings"). The ValueComparer tells EF
    /// Core's change tracker how to check if the list actually changed -
    /// without it, EF Core would compare list references, and could miss
    /// (or wrongly detect) changes made by mutating the same list object.
    /// </summary>
    private static void ConfigureJsonStringList(PropertyBuilder<List<string>> property)
    {
        property
            .HasConversion(
                list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                // Guard against a real database NULL, not just a "[]" json
                // string - the column itself is nullable (see IsRequired
                // below), even though our own C# property always defaults
                // to an empty, non-null list.
                json => string.IsNullOrEmpty(json)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (a, b) => a!.SequenceEqual(b!),
                list => list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                list => list.ToList()));

        // The blueprint marks Inclusions/Exclusions NULL - override EF
        // Core's convention (which would infer NOT NULL purely because
        // List<string> is a non-nullable CLR type) to match it exactly.
        property.IsRequired(false);
    }
}

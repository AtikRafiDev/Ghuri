using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class DestinationConfiguration : IEntityTypeConfiguration<Destination>
{
    public void Configure(EntityTypeBuilder<Destination> builder)
    {
        builder.ToTable("Destinations", schema: "catalog");

        builder.HasKey(d => d.Id);
        builder.HasAuditColumns();
        builder.HasSoftDeleteFilter();

        builder.Property(d => d.CountryId).IsRequired();
        builder.HasOne<Country>()
            .WithMany()
            .HasForeignKey(d => d.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(d => d.Name).HasMaxLength(150).IsRequired();

        builder.Property(d => d.Slug)
            .HasConversion(s => s.Value, v => Slug.Create(v))
            .HasMaxLength(160)
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(d => d.Slug).IsUnique().HasFilter(SoftDeleteConfigurationExtensions.NotDeleted);

        builder.Property(d => d.Summary).HasMaxLength(500);

        // The photo gallery (catalog.DestinationImages). EF reads and writes
        // the private _images list directly - the public Images property is
        // a sorted read-only copy. Cascade: the rows go if the destination
        // is ever really deleted (a soft delete keeps them, like everything else).
        builder.HasMany(d => d.Images)
            .WithOne()
            .HasForeignKey(i => i.DestinationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(d => d.Images).HasField("_images").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(d => d.IsFeatured).IsRequired();
        builder.HasIndex(d => d.IsFeatured);

        builder.Property(d => d.SortOrder).IsRequired();
        builder.Property(d => d.SeoTitle).HasMaxLength(70);
        builder.Property(d => d.SeoDescription).HasMaxLength(160);
    }
}

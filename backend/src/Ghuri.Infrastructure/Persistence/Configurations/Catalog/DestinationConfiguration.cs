using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Ops;
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
        // Was a plain column with no FK while ops hadn't been built yet -
        // now that FileObject exists, the real constraint goes here.
        builder.Property(d => d.ImageFileId);
        builder.HasOne<FileObject>().WithMany().HasForeignKey(d => d.ImageFileId).OnDelete(DeleteBehavior.SetNull);

        builder.Property(d => d.IsFeatured).IsRequired();
        builder.HasIndex(d => d.IsFeatured);

        builder.Property(d => d.SortOrder).IsRequired();
        builder.Property(d => d.SeoTitle).HasMaxLength(70);
        builder.Property(d => d.SeoDescription).HasMaxLength(160);
    }
}

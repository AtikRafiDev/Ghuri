using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class PackageImageConfiguration : IEntityTypeConfiguration<PackageImage>
{
    public void Configure(EntityTypeBuilder<PackageImage> builder)
    {
        builder.ToTable("PackageImages", schema: "catalog");

        builder.HasKey(pi => pi.Id);

        builder.Property(pi => pi.PackageId).IsRequired();
        // Two DIFFERENT indexes on the same PackageId column, so each
        // needs its name passed directly to HasIndex - if both calls used
        // the default name, EF Core would treat the second call as just
        // reconfiguring the first index instead of creating a second one.
        builder.HasIndex(pi => pi.PackageId, "IX_PackageImages_PackageId");

        // Was a plain column with no FK while ops hadn't been built yet -
        // now that FileObject exists, the real constraint goes here.
        // Restrict, not SetNull: this column is required (an image with no
        // file makes no sense), so the file can't be deleted while any
        // image still points to it.
        builder.Property(pi => pi.FileId).IsRequired();
        builder.HasOne<FileObject>().WithMany().HasForeignKey(pi => pi.FileId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(pi => pi.Caption).HasMaxLength(200);
        builder.Property(pi => pi.SortOrder).IsRequired();

        builder.Property(pi => pi.IsCover).IsRequired();
        // "Exactly one cover image" per package - a filtered unique index
        // on (PackageId) that only counts rows WHERE IsCover = 1, so any
        // number of non-cover images is fine but a second cover for the
        // same package is rejected by the database itself.
        builder.HasIndex(pi => pi.PackageId, "IX_PackageImages_PackageId_OneCover")
            .IsUnique()
            .HasFilter("[IsCover] = 1");
    }
}

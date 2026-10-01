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
        builder.HasIndex(pi => pi.PackageId, "IX_PackageImages_PackageId");

        // Was a plain column with no FK while ops hadn't been built yet -
        // now that FileObject exists, the real constraint goes here.
        // Restrict, not SetNull: this column is required (an image with no
        // file makes no sense), so the file can't be deleted while any
        // image still points to it.
        builder.Property(pi => pi.FileId).IsRequired();
        builder.HasOne<FileObject>().WithMany().HasForeignKey(pi => pi.FileId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(pi => pi.Caption).HasMaxLength(200);
        // SortOrder 0 = the cover, like DestinationImages. There used to be
        // an IsCover column with a "one cover per package" unique index, but
        // moving the cover to another photo then needs two UPDATEs in a
        // strict order (old cover off first), and EF Core doesn't promise
        // that order - the save could fail at random.
        builder.Property(pi => pi.SortOrder).IsRequired();
    }
}

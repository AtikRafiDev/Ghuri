using Ghuri.Domain.Entities.Cms;
using Ghuri.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Cms;

internal sealed class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder)
    {
        builder.ToTable("Banners", schema: "cms");

        builder.HasKey(b => b.Id);
        builder.HasAuditColumns();
        builder.HasSoftDeleteFilter();

        builder.Property(b => b.Title).HasMaxLength(150).IsRequired();
        builder.Property(b => b.Subtitle).HasMaxLength(300);

        // Was a plain column with no FK while ops hadn't been built yet -
        // now that FileObject exists, the real constraint goes here.
        // Required column, so Restrict, not SetNull.
        builder.Property(b => b.ImageFileId).IsRequired();
        builder.HasOne<FileObject>().WithMany().HasForeignKey(b => b.ImageFileId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(b => b.LinkUrl).HasMaxLength(500);

        builder.Property(b => b.Placement).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Banners_Placement", "[Placement] IN (1,2,3)"));
        builder.HasIndex(b => b.Placement);

        builder.Property(b => b.SortOrder).IsRequired();
        builder.Property(b => b.StartsAtUtc);
        builder.Property(b => b.EndsAtUtc);
        builder.Property(b => b.IsActive).IsRequired();
    }
}

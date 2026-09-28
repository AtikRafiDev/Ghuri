using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Marketing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Marketing;

internal sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("Coupons", schema: "marketing");

        builder.HasKey(c => c.Id);
        builder.HasAuditColumns();
        builder.HasSoftDeleteFilter();

        builder.Property(c => c.Code).HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.HasIndex(c => c.Code).IsUnique();

        builder.Property(c => c.Description).HasMaxLength(300);

        builder.Property(c => c.DiscountType).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Coupons_DiscountType", "[DiscountType] IN (1,2)"));

        builder.Property(c => c.Value).HasColumnType("decimal(18,2)").IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Coupons_Value", "[Value] > 0"));

        builder.Property(c => c.MaxDiscount).HasColumnType("decimal(18,2)");
        builder.Property(c => c.MinOrderAmount).HasColumnType("decimal(18,2)").IsRequired();

        builder.Property(c => c.ValidFromUtc).IsRequired();
        builder.Property(c => c.ValidToUtc).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Coupons_ValidToUtc", "[ValidToUtc] > [ValidFromUtc]"));

        builder.Property(c => c.MaxUses);
        builder.Property(c => c.MaxUsesPerUser).IsRequired();
        builder.Property(c => c.UsedCount).IsRequired();

        builder.Property(c => c.PackageId); // null = applies to every package
        builder.HasOne<TourPackage>().WithMany().HasForeignKey(c => c.PackageId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.IsActive).IsRequired();

        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}

using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Entities.Marketing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BookingEntity = Ghuri.Domain.Entities.Booking.Booking;

namespace Ghuri.Infrastructure.Persistence.Configurations.Marketing;

internal sealed class CouponRedemptionConfiguration : IEntityTypeConfiguration<CouponRedemption>
{
    public void Configure(EntityTypeBuilder<CouponRedemption> builder)
    {
        builder.ToTable("CouponRedemptions", schema: "marketing");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.CouponId).IsRequired();
        builder.HasOne<Coupon>().WithMany().HasForeignKey(r => r.CouponId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => new { r.CouponId, r.UserId });

        builder.Property(r => r.BookingId).IsRequired();
        builder.HasOne<BookingEntity>().WithMany().HasForeignKey(r => r.BookingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.BookingId).IsUnique();

        builder.Property(r => r.UserId).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.DiscountAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(r => r.RedeemedAtUtc).IsRequired();
    }
}

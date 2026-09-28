using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Entities.Marketing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BookingEntity = Ghuri.Domain.Entities.Booking.Booking;

namespace Ghuri.Infrastructure.Persistence.Configurations.Marketing;

internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews", schema: "marketing");

        builder.HasKey(r => r.Id);
        builder.HasAuditColumns();

        builder.Property(r => r.PackageId).IsRequired();
        builder.HasOne<TourPackage>().WithMany().HasForeignKey(r => r.PackageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.PackageId);

        builder.Property(r => r.BookingId).IsRequired();
        builder.HasOne<BookingEntity>().WithMany().HasForeignKey(r => r.BookingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.BookingId).IsUnique(); // one review per booking

        builder.Property(r => r.UserId).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.Rating).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Reviews_Rating", "[Rating] BETWEEN 1 AND 5"));

        builder.Property(r => r.Title).HasMaxLength(150);
        builder.Property(r => r.Comment).HasMaxLength(2000);

        builder.Property(r => r.Status).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Reviews_Status", "[Status] IN (1,2,3)"));

        builder.Property(r => r.ModeratedBy);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.ModeratedBy).OnDelete(DeleteBehavior.Restrict);
        builder.Property(r => r.ModeratedAtUtc);
    }
}

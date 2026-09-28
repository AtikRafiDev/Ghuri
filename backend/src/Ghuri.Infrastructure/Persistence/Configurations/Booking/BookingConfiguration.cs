using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BookingEntity = Ghuri.Domain.Entities.Booking.Booking;

namespace Ghuri.Infrastructure.Persistence.Configurations.Booking;

internal sealed class BookingConfiguration : IEntityTypeConfiguration<BookingEntity>
{
    public void Configure(EntityTypeBuilder<BookingEntity> builder)
    {
        builder.ToTable("Bookings", schema: "booking");

        builder.HasKey(b => b.Id);
        builder.HasAuditColumns();

        builder.Property(b => b.BookingNo).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.HasIndex(b => b.BookingNo).IsUnique();

        builder.Property(b => b.CustomerId).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(b => b.CustomerId).OnDelete(DeleteBehavior.Restrict);
        // Composite index mixing a real property (CustomerId) with a shadow
        // property (CreatedAtUtc, added by [A]) - the string-array overload
        // of HasIndex works for both kinds by property NAME.
        builder.HasIndex(new[] { nameof(BookingEntity.CustomerId), "CreatedAtUtc" });

        builder.Property(b => b.DepartureId).IsRequired();
        builder.HasOne<Departure>().WithMany().HasForeignKey(b => b.DepartureId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(b => b.DepartureId);

        builder.Property(b => b.PackageId).IsRequired(); // denormalized for reports, per the blueprint - no FK-driven cascading behaviour needed beyond referential integrity
        builder.HasOne<TourPackage>().WithMany().HasForeignKey(b => b.PackageId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(b => b.Adults).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Bookings_Adults", "[Adults] >= 1"));
        builder.Property(b => b.Children).IsRequired();
        builder.Property(b => b.Infants).IsRequired();

        builder.Property(b => b.AdultPriceSnapshot).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(b => b.ChildPriceSnapshot).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(b => b.InfantPriceSnapshot).HasColumnType("decimal(18,2)").IsRequired();

        builder.Property(b => b.SubTotal).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(b => b.AddOnTotal).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(b => b.DiscountAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(b => b.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Bookings_TotalAmount",
            "[TotalAmount] = [SubTotal] + [AddOnTotal] - [DiscountAmount]"));

        builder.Property(b => b.PaidAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Bookings_PaidAmount", "[PaidAmount] <= [TotalAmount]"));

        builder.Property(b => b.Currency).HasMaxLength(3).IsFixedLength().IsUnicode(false).IsRequired();

        builder.Property(b => b.PaymentPlan).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Bookings_PaymentPlan", "[PaymentPlan] IN (1,2)"));

        builder.Property(b => b.BalanceDueDate).HasColumnType("date");

        builder.Property(b => b.CouponId); // plain column, no FK - marketing.Coupons doesn't exist yet

        builder.Property(b => b.Status).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Bookings_Status", "[Status] IN (1,2,3,4,5,6)"));
        builder.HasIndex(b => b.Status);

        builder.Property(b => b.HoldExpiresAtUtc);
        // "1" here is BookingStatus.PendingPayment's stored value - the
        // filter is raw SQL against the TINYINT column, not the C# enum.
        builder.HasIndex(b => b.HoldExpiresAtUtc).HasFilter("[Status] = 1");

        builder.Property(b => b.ContactName).HasMaxLength(150).IsRequired();

        builder.Property(b => b.ContactPhone)
            .HasConversion(p => p.Value, v => PhoneNumber.Create(v))
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(b => b.ContactEmail).HasMaxLength(256);
        builder.Property(b => b.SpecialRequest).HasMaxLength(1000);

        builder.Property(b => b.Source).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Bookings_Source", "[Source] IN (1,2)"));

        builder.Property(b => b.CancelledAtUtc);
        builder.Property(b => b.CancelReason).HasMaxLength(500);

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasMany(b => b.Travellers).WithOne().HasForeignKey(t => t.BookingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(b => b.AddOns).WithOne().HasForeignKey(a => a.BookingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(b => b.History).WithOne().HasForeignKey(h => h.BookingId).OnDelete(DeleteBehavior.Cascade);
    }
}

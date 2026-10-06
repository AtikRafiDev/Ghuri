using Ghuri.Domain.Entities.Booking;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Entities.Marketing;
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

        builder.Property(b => b.BookingType).HasConversion<byte>().IsRequired();

        // Optional since bookings can be flexible stays (no departure) or
        // custom trips (no package either) - CK_Bookings_Shape below says
        // exactly which one each type needs.
        builder.HasOne<Departure>().WithMany().HasForeignKey(b => b.DepartureId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(b => b.DepartureId);

        // Denormalized for reports, per the blueprint - no FK-driven cascading behaviour needed beyond referential integrity.
        builder.HasOne<TourPackage>().WithMany().HasForeignKey(b => b.PackageId).OnDelete(DeleteBehavior.Restrict);

        // An accepted custom-trip quote becomes a booking (Day 15).
        builder.HasOne<CustomTrip>().WithMany().HasForeignKey(b => b.CustomTripId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(b => b.CustomTripId);

        // Which links each type must have - the same rule BookingStay
        // enforces in C#. The numbers are BookingType's stored values
        // (1 fixed, 2 flexible, 3 custom trip), so this also rejects any
        // other type. A flexible stay is at least one night.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Bookings_Shape",
            "([BookingType] = 1 AND [DepartureId] IS NOT NULL AND [PackageId] IS NOT NULL AND [CustomTripId] IS NULL)" +
            " OR ([BookingType] = 2 AND [DepartureId] IS NULL AND [PackageId] IS NOT NULL AND [CustomTripId] IS NULL AND [Nights] >= 1)" +
            " OR ([BookingType] = 3 AND [DepartureId] IS NULL AND [PackageId] IS NULL AND [CustomTripId] IS NOT NULL)"));

        // The trip's dates, stored for every type (see Booking's remarks).
        // Indexed for "upcoming trips" (admin dashboard, reminders).
        builder.Property(b => b.StartDate).HasColumnType("date").IsRequired();
        builder.HasIndex(b => b.StartDate);
        builder.Property(b => b.EndDate).HasColumnType("date").IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Bookings_Dates", "[EndDate] >= [StartDate]"));
        builder.Property(b => b.Nights).IsRequired();

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

        builder.Property(b => b.CouponId);
        // Was a plain column with no FK while marketing hadn't been built
        // yet - now that Coupon exists, the real constraint goes here.
        // Restrict, not Cascade: a coupon being deactivated should never
        // silently delete someone's booking history.
        builder.HasOne<Coupon>().WithMany().HasForeignKey(b => b.CouponId).OnDelete(DeleteBehavior.Restrict);

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

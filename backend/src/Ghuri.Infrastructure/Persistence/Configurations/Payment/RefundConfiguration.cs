using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Entities.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BookingEntity = Ghuri.Domain.Entities.Booking.Booking;
using PaymentEntity = Ghuri.Domain.Entities.Payment.Payment;

namespace Ghuri.Infrastructure.Persistence.Configurations.Payment;

internal sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("Refunds", schema: "payment");

        builder.HasKey(r => r.Id);
        builder.HasAuditColumns();

        builder.Property(r => r.RefundNo).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.HasIndex(r => r.RefundNo).IsUnique();

        builder.Property(r => r.BookingId).IsRequired();
        builder.HasOne<BookingEntity>().WithMany().HasForeignKey(r => r.BookingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.BookingId);

        builder.Property(r => r.PaymentId).IsRequired();
        builder.HasOne<PaymentEntity>().WithMany().HasForeignKey(r => r.PaymentId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.Amount).HasColumnType("decimal(18,2)").IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Refunds_Amount", "[Amount] > 0"));

        builder.Property(r => r.RefundPercent).HasColumnType("decimal(5,2)").IsRequired();
        builder.Property(r => r.Reason).HasMaxLength(500).IsRequired();

        builder.Property(r => r.Status).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Refunds_Status", "[Status] IN (1,2,3,4,5,6)"));
        builder.HasIndex(r => r.Status);

        // Two separate FKs to the SAME table (iam.Users) - each needs its
        // own explicit relationship, otherwise EF Core can't tell which
        // column is which.
        // Optional: null = requested by the system (a late or double payment - Day 12).
        builder.Property(r => r.RequestedBy);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.RequestedBy).OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.ApprovedBy);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.ApprovedBy).OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.ApprovedAtUtc);
        builder.Property(r => r.ProviderRefundId).HasMaxLength(100).IsUnicode(false);
        builder.Property(r => r.CompletedAtUtc);
        builder.Property(r => r.FailureReason).HasMaxLength(300);

        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}

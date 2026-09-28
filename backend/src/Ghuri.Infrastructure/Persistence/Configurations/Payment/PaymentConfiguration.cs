using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BookingEntity = Ghuri.Domain.Entities.Booking.Booking;
using PaymentEntity = Ghuri.Domain.Entities.Payment.Payment;

namespace Ghuri.Infrastructure.Persistence.Configurations.Payment;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<PaymentEntity>
{
    public void Configure(EntityTypeBuilder<PaymentEntity> builder)
    {
        builder.ToTable("Payments", schema: "payment");

        builder.HasKey(p => p.Id);
        builder.HasAuditColumns();

        builder.Property(p => p.PaymentNo).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.HasIndex(p => p.PaymentNo).IsUnique();

        builder.Property(p => p.BookingId).IsRequired();
        builder.HasOne<BookingEntity>().WithMany().HasForeignKey(p => p.BookingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.BookingId);

        builder.Property(p => p.Provider).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Payments_Provider", "[Provider] IN (1,2,3)"));

        builder.Property(p => p.Method).HasMaxLength(30);

        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)").IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Payments_Amount", "[Amount] > 0"));

        builder.Property(p => p.Currency).HasMaxLength(3).IsFixedLength().IsUnicode(false).IsRequired();

        builder.Property(p => p.Status).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Payments_Status", "[Status] IN (1,2,3,4,5,6,7)"));
        builder.HasIndex(p => p.Status);

        builder.Property(p => p.ProviderSessionId).HasMaxLength(100).IsUnicode(false);

        builder.Property(p => p.ProviderTransactionId).HasMaxLength(100).IsUnicode(false);
        builder.HasIndex(p => p.ProviderTransactionId).IsUnique().HasFilter("[ProviderTransactionId] IS NOT NULL");

        builder.Property(p => p.GatewayFee).HasColumnType("decimal(18,2)");
        builder.Property(p => p.InitiatedAtUtc).IsRequired();
        builder.Property(p => p.PaidAtUtc);
        builder.Property(p => p.FailureReason).HasMaxLength(300);

        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}

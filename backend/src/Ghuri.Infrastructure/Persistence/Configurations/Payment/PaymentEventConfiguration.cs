using Ghuri.Domain.Entities.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentEntity = Ghuri.Domain.Entities.Payment.Payment;

namespace Ghuri.Infrastructure.Persistence.Configurations.Payment;

internal sealed class PaymentEventConfiguration : IEntityTypeConfiguration<PaymentEvent>
{
    public void Configure(EntityTypeBuilder<PaymentEvent> builder)
    {
        builder.ToTable("PaymentEvents", schema: "payment");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityColumn();

        builder.Property(e => e.PaymentId);
        builder.HasOne<PaymentEntity>().WithMany().HasForeignKey(e => e.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.PaymentId);

        builder.Property(e => e.Provider).HasConversion<byte>().IsRequired();

        builder.Property(e => e.EventType).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_PaymentEvents_EventType", "[EventType] IN (1,2,3,4,5)"));

        builder.Property(e => e.ProviderEventId).HasMaxLength(100).IsUnicode(false).IsRequired();
        // "Duplicate IPNs are rejected by this index" - the blueprint's
        // idempotency guarantee for gateway callbacks lives here, not in
        // application code: the database itself refuses a second row for
        // the same (Provider, ProviderEventId) pair.
        builder.HasIndex(e => new { e.Provider, e.ProviderEventId }).IsUnique();

        builder.Property(e => e.PayloadJson).IsRequired(); // NVARCHAR(MAX)
        builder.Property(e => e.ReceivedAtUtc).IsRequired();
        builder.Property(e => e.ProcessedAtUtc);
        builder.Property(e => e.ProcessingResult).HasMaxLength(300);
    }
}

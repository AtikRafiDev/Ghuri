using Ghuri.Domain.Entities.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Booking;

internal sealed class BookingStatusHistoryConfiguration : IEntityTypeConfiguration<BookingStatusHistory>
{
    public void Configure(EntityTypeBuilder<BookingStatusHistory> builder)
    {
        builder.ToTable("BookingStatusHistory", schema: "booking");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).UseIdentityColumn();

        builder.Property(h => h.BookingId).IsRequired();
        builder.HasIndex(h => h.BookingId);

        builder.Property(h => h.FromStatus).HasConversion<byte?>();
        builder.Property(h => h.ToStatus).HasConversion<byte>().IsRequired();

        builder.Property(h => h.ChangedAtUtc).IsRequired();
        builder.Property(h => h.ChangedBy); // null = a background job made the change
        builder.Property(h => h.Note).HasMaxLength(500);
    }
}

using Ghuri.Domain.Entities.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Booking;

internal sealed class BookingTravellerConfiguration : IEntityTypeConfiguration<BookingTraveller>
{
    public void Configure(EntityTypeBuilder<BookingTraveller> builder)
    {
        builder.ToTable("BookingTravellers", schema: "booking");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.BookingId).IsRequired();
        builder.HasIndex(t => t.BookingId);

        builder.Property(t => t.TravellerType).HasConversion<byte>().IsRequired();
        builder.ToTable(tb => tb.HasCheckConstraint("CK_BookingTravellers_TravellerType", "[TravellerType] IN (1,2,3)"));

        builder.Property(t => t.FullName).HasMaxLength(150).IsRequired();

        builder.Property(t => t.Gender).HasConversion<byte?>();
        builder.ToTable(tb => tb.HasCheckConstraint(
            "CK_BookingTravellers_Gender",
            "[Gender] IS NULL OR [Gender] IN (1,2,3)"));

        builder.Property(t => t.DateOfBirth).HasColumnType("date");
        builder.Property(t => t.Nationality).HasMaxLength(2).IsFixedLength().IsUnicode(false);

        builder.Property(t => t.PassportNo).HasMaxLength(128);
        builder.Property(t => t.Phone).HasMaxLength(20).IsUnicode(false);
        builder.Property(t => t.IsLead).IsRequired();
    }
}

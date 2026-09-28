using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Booking;

internal sealed class BookingAddOnConfiguration : IEntityTypeConfiguration<BookingAddOn>
{
    public void Configure(EntityTypeBuilder<BookingAddOn> builder)
    {
        builder.ToTable("BookingAddOns", schema: "booking");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.BookingId).IsRequired();

        builder.Property(a => a.AddOnId).IsRequired();
        builder.HasOne<PackageAddOn>()
            .WithMany()
            .HasForeignKey(a => a.AddOnId)
            .OnDelete(DeleteBehavior.Restrict); // never delete an add-on definition while a booking's receipt still references it

        builder.Property(a => a.NameSnapshot).HasMaxLength(150).IsRequired();
        builder.Property(a => a.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();

        builder.Property(a => a.Quantity).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_BookingAddOns_Quantity", "[Quantity] > 0"));

        builder.Property(a => a.LineTotal).HasColumnType("decimal(18,2)").IsRequired();
    }
}

using Ghuri.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class DepartureConfiguration : IEntityTypeConfiguration<Departure>
{
    public void Configure(EntityTypeBuilder<Departure> builder)
    {
        builder.ToTable("Departures", schema: "catalog");
        builder.HasAuditColumns();

        builder.HasKey(d => d.Id);

        builder.Property(d => d.PackageId).IsRequired();
        // Departure is its OWN aggregate root (see the class remarks), not
        // owned by TourPackage, so the relationship is configured here,
        // not from TourPackageConfiguration.
        builder.HasOne<TourPackage>()
            .WithMany()
            .HasForeignKey(d => d.PackageId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(d => new { d.PackageId, d.StartDate }).IsUnique(); // one departure per package per day

        builder.Property(d => d.StartDate).HasColumnType("date").IsRequired();
        builder.HasIndex(d => d.StartDate);

        builder.Property(d => d.EndDate).HasColumnType("date").IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Departures_EndDate", "[EndDate] >= [StartDate]"));

        builder.Property(d => d.AdultPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(d => d.ChildPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(d => d.InfantPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Departures_PricesNonNegative",
            "[AdultPrice] >= 0 AND [ChildPrice] >= 0 AND [InfantPrice] >= 0"));

        builder.Property(d => d.SingleSupplement).HasColumnType("decimal(18,2)");

        builder.Property(d => d.TotalSeats).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Departures_TotalSeats", "[TotalSeats] > 0"));

        builder.Property(d => d.ReservedSeats).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Departures_ReservedSeats", "[ReservedSeats] <= [TotalSeats]"));

        builder.Property(d => d.BookingCutoffDays).IsRequired();

        builder.Property(d => d.Status).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Departures_Status", "[Status] IN (1,2,3,4)"));
        builder.HasIndex(d => d.Status);

        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}

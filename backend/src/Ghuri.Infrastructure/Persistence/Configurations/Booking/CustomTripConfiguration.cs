using Ghuri.Domain.Entities.Booking;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Booking;

/// <summary>booking.CustomTrips - a multi-destination request and its current quote (Day 13).</summary>
internal sealed class CustomTripConfiguration : IEntityTypeConfiguration<CustomTrip>
{
    public void Configure(EntityTypeBuilder<CustomTrip> builder)
    {
        builder.ToTable("CustomTrips", schema: "booking");

        builder.HasKey(t => t.Id);
        builder.HasAuditColumns();

        builder.Property(t => t.TripNo).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.HasIndex(t => t.TripNo).IsUnique();

        builder.Property(t => t.CustomerId).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(t => t.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.CustomerId);

        builder.Property(t => t.Status).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_CustomTrips_Status", "[Status] BETWEEN 1 AND 7"));
        // The staff queue: "waiting for a quote, oldest first".
        builder.HasIndex(t => new { t.Status, t.SubmittedAtUtc });

        builder.Property(t => t.StartDate).IsRequired();
        builder.Property(t => t.EndDate).IsRequired();
        builder.Property(t => t.TotalNights).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_CustomTrips_Dates", "[EndDate] > [StartDate]"));

        builder.Property(t => t.Adults).IsRequired();
        builder.Property(t => t.Children).IsRequired();
        builder.Property(t => t.Infants).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_CustomTrips_Adults", "[Adults] >= 1"));

        builder.Property(t => t.HotelLevel).HasConversion<byte>().IsRequired();
        builder.Property(t => t.BudgetPerPerson).HasColumnType("decimal(18,2)");
        builder.Property(t => t.Notes).HasMaxLength(2000);

        builder.Property(t => t.ContactName).HasMaxLength(150).IsRequired();
        builder.Property(t => t.ContactPhone)
            .HasConversion(p => p.Value, v => PhoneNumber.Create(v))
            .HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(t => t.ContactEmail).HasMaxLength(256);

        builder.Property(t => t.Currency).HasMaxLength(3).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(t => t.SubmittedAtUtc).IsRequired();

        builder.Property(t => t.QuoteVersion).IsRequired();
        builder.Property(t => t.QuoteItinerary).HasMaxLength(4000);
        builder.Property(t => t.QuoteTotal).HasColumnType("decimal(18,2)");
        builder.Property(t => t.QuotedBy);
        builder.HasOne<User>().WithMany().HasForeignKey(t => t.QuotedBy).OnDelete(DeleteBehavior.Restrict);
        // The hourly job: "quotes past their deadline".
        builder.HasIndex(t => t.QuoteExpiresAtUtc).HasFilter("[Status] = 2");

        builder.Property(t => t.RejectReason).HasMaxLength(500);
        builder.Property(t => t.CancelReason).HasMaxLength(500);

        builder.HasMany(t => t.Legs).WithOne().HasForeignKey(l => l.CustomTripId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(t => t.Legs).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(t => t.QuoteLines).WithOne().HasForeignKey(l => l.CustomTripId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(t => t.QuoteLines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}

/// <summary>booking.CustomTripLegs - the stops of a custom trip, in order.</summary>
internal sealed class CustomTripLegConfiguration : IEntityTypeConfiguration<CustomTripLeg>
{
    public void Configure(EntityTypeBuilder<CustomTripLeg> builder)
    {
        builder.ToTable("CustomTripLegs", schema: "booking");

        builder.HasKey(l => l.Id);
        builder.HasIndex(l => new { l.CustomTripId, l.Sequence }).IsUnique();

        builder.Property(l => l.DestinationId).IsRequired();
        builder.HasOne<Destination>().WithMany().HasForeignKey(l => l.DestinationId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.Nights).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_CustomTripLegs_Nights", "[Nights] >= 1"));
        builder.Property(l => l.TransferToNext).HasConversion<byte>().IsRequired();
        builder.Property(l => l.CheckInDate).IsRequired();
        builder.Property(l => l.CheckOutDate).IsRequired();
    }
}

/// <summary>booking.CustomTripQuoteLines - the price lines of the current quote.</summary>
internal sealed class CustomTripQuoteLineConfiguration : IEntityTypeConfiguration<CustomTripQuoteLine>
{
    public void Configure(EntityTypeBuilder<CustomTripQuoteLine> builder)
    {
        builder.ToTable("CustomTripQuoteLines", schema: "booking");

        builder.HasKey(l => l.Id);
        builder.HasIndex(l => new { l.CustomTripId, l.Sequence }).IsUnique();

        builder.Property(l => l.Category).HasConversion<byte>().IsRequired();
        builder.Property(l => l.Description).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Amount).HasColumnType("decimal(18,2)").IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_CustomTripQuoteLines_Amount", "[Amount] > 0"));
    }
}

using Ghuri.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class ItineraryDayConfiguration : IEntityTypeConfiguration<ItineraryDay>
{
    public void Configure(EntityTypeBuilder<ItineraryDay> builder)
    {
        builder.ToTable("ItineraryDays", schema: "catalog");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.PackageId).IsRequired();
        builder.HasIndex(d => new { d.PackageId, d.DayNo }).IsUnique(); // one row per day number per package

        builder.Property(d => d.DayNo).IsRequired();
        builder.Property(d => d.Title).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Description).IsRequired(); // NVARCHAR(MAX) by default when no HasMaxLength is set
        builder.Property(d => d.Meals).HasMaxLength(10).IsUnicode(false);
        builder.Property(d => d.Accommodation).HasMaxLength(150);
    }
}

using Ghuri.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Ops;

internal sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings", schema: "ops");

        // Also a string primary key - "Booking.HoldMinutes" etc.
        builder.HasKey(s => s.Key);
        builder.Property(s => s.Key).HasMaxLength(100).IsUnicode(false);

        builder.Property(s => s.Value).HasMaxLength(1000).IsRequired();
        builder.Property(s => s.ValueType).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(300);
        builder.Property(s => s.UpdatedAtUtc).IsRequired();
        builder.Property(s => s.UpdatedBy);
    }
}

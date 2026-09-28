using Ghuri.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class PackageAddOnConfiguration : IEntityTypeConfiguration<PackageAddOn>
{
    public void Configure(EntityTypeBuilder<PackageAddOn> builder)
    {
        builder.ToTable("PackageAddOns", schema: "catalog");
        builder.HasAuditColumns();

        builder.HasKey(a => a.Id);

        builder.Property(a => a.PackageId).IsRequired();
        builder.HasIndex(a => a.PackageId);

        builder.Property(a => a.Name).HasMaxLength(150).IsRequired();

        builder.Property(a => a.Price).HasColumnType("decimal(18,2)").IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_PackageAddOns_Price", "[Price] >= 0"));

        builder.Property(a => a.PricingUnit).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_PackageAddOns_PricingUnit", "[PricingUnit] IN (1,2)"));

        builder.Property(a => a.IsActive).IsRequired();
    }
}

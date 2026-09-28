using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Booking;

internal sealed class CancellationPolicyConfiguration : IEntityTypeConfiguration<CancellationPolicy>
{
    public void Configure(EntityTypeBuilder<CancellationPolicy> builder)
    {
        builder.ToTable("CancellationPolicies", schema: "booking");
        builder.HasAuditColumns();

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PackageId); // null = global default policy
        builder.HasOne<TourPackage>()
            .WithMany()
            .HasForeignKey(p => p.PackageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Two separate unique indexes, not one. SQL Server (and EF Core's
        // own default convention) treats every NULL as distinct from every
        // other NULL, so a single unique index on (PackageId, MinDaysBefore)
        // would silently allow TWO global-default rows (PackageId = NULL)
        // for the same MinDaysBefore - exactly the duplicate the blueprint's
        // uniqueness rule is meant to prevent. Splitting it into "unique
        // per package" and "unique among the global defaults" closes that
        // gap in both directions.
        builder.HasIndex(p => new { p.PackageId, p.MinDaysBefore }, "IX_CancellationPolicies_PackageId_MinDaysBefore")
            .IsUnique()
            .HasFilter("[PackageId] IS NOT NULL");

        builder.HasIndex(p => p.MinDaysBefore, "IX_CancellationPolicies_GlobalDefault_MinDaysBefore")
            .IsUnique()
            .HasFilter("[PackageId] IS NULL");

        builder.Property(p => p.MinDaysBefore).IsRequired();

        builder.Property(p => p.RefundPercent).HasColumnType("decimal(5,2)").IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_CancellationPolicies_RefundPercent", "[RefundPercent] BETWEEN 0 AND 100"));
    }
}

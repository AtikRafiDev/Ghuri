using Ghuri.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class PackageCategoryConfiguration : IEntityTypeConfiguration<PackageCategory>
{
    public void Configure(EntityTypeBuilder<PackageCategory> builder)
    {
        builder.ToTable("PackageCategories", schema: "catalog");

        builder.HasKey(pc => new { pc.PackageId, pc.CategoryId });

        builder.HasOne<TourPackage>()
            .WithMany(p => p.Categories) // TourPackage.SetCategories adds/removes these rows
            .HasForeignKey(pc => pc.PackageId)
            .OnDelete(DeleteBehavior.Cascade); // deleting a package removes its category links

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(pc => pc.CategoryId)
            .OnDelete(DeleteBehavior.Restrict); // never delete a Category while a package still uses it
    }
}

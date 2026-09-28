using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Entities.Marketing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Marketing;

internal sealed class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.ToTable("Wishlists", schema: "marketing");

        builder.HasKey(w => new { w.UserId, w.PackageId });

        builder.HasOne<User>().WithMany().HasForeignKey(w => w.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<TourPackage>().WithMany().HasForeignKey(w => w.PackageId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(w => w.CreatedAtUtc).IsRequired();
    }
}

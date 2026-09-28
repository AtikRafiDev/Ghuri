using Ghuri.Domain.Entities.Cms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Cms;

internal sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems", schema: "cms");
        builder.HasAuditColumns();

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Location).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_MenuItems_Location", "[Location] IN (1,2)"));

        builder.Property(m => m.Label).HasMaxLength(80).IsRequired();
        builder.Property(m => m.Url).HasMaxLength(300).IsRequired();

        // Self-referencing FK: a menu item can nest under another one.
        // Restrict, not Cascade - deleting a parent should never silently
        // delete every menu item nested under it.
        builder.Property(m => m.ParentId);
        builder.HasOne<MenuItem>().WithMany().HasForeignKey(m => m.ParentId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(m => m.SortOrder).IsRequired();
    }
}

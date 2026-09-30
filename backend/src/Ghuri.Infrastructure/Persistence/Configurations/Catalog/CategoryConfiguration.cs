using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", schema: "catalog");

        builder.HasKey(c => c.Id);
        builder.HasAuditColumns();
        builder.HasSoftDeleteFilter();

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(c => c.Name).IsUnique().HasFilter(SoftDeleteConfigurationExtensions.NotDeleted);

        builder.Property(c => c.Slug)
            .HasConversion(s => s.Value, v => Slug.Create(v))
            .HasMaxLength(120)
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(c => c.Slug).IsUnique().HasFilter(SoftDeleteConfigurationExtensions.NotDeleted);

        builder.Property(c => c.Icon).HasMaxLength(50).IsUnicode(false);
        builder.Property(c => c.SortOrder).IsRequired();
    }
}

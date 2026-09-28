using Ghuri.Domain.Entities.Cms;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Cms;

internal sealed class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.ToTable("Pages", schema: "cms");

        builder.HasKey(p => p.Id);
        builder.HasAuditColumns();
        builder.HasSoftDeleteFilter();

        builder.Property(p => p.Slug)
            .HasConversion(s => s.Value, v => Slug.Create(v))
            .HasMaxLength(120)
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(p => p.Slug).IsUnique();

        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Content).IsRequired(); // NVARCHAR(MAX)

        builder.Property(p => p.SeoTitle).HasMaxLength(70);
        builder.Property(p => p.SeoDescription).HasMaxLength(160);
        builder.Property(p => p.IsPublished).IsRequired();
    }
}

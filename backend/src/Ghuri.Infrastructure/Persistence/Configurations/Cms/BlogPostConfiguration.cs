using Ghuri.Domain.Entities.Cms;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Entities.Ops;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Cms;

internal sealed class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.ToTable("BlogPosts", schema: "cms");

        builder.HasKey(p => p.Id);
        builder.HasAuditColumns();
        builder.HasSoftDeleteFilter();

        builder.Property(p => p.Slug)
            .HasConversion(s => s.Value, v => Slug.Create(v))
            .HasMaxLength(220)
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(p => p.Slug).IsUnique();

        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Excerpt).HasMaxLength(500);
        builder.Property(p => p.Content).IsRequired(); // NVARCHAR(MAX)

        // Was a plain column with no FK while ops hadn't been built yet -
        // now that FileObject exists, the real constraint goes here.
        builder.Property(p => p.CoverFileId);
        builder.HasOne<FileObject>().WithMany().HasForeignKey(p => p.CoverFileId).OnDelete(DeleteBehavior.SetNull);

        builder.Property(p => p.AuthorId).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(p => p.AuthorId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.Tags).HasMaxLength(300);

        builder.Property(p => p.IsPublished).IsRequired();
        builder.Property(p => p.PublishedAtUtc);
        builder.HasIndex(p => new { p.IsPublished, p.PublishedAtUtc });

        builder.Property(p => p.SeoTitle).HasMaxLength(70);
        builder.Property(p => p.SeoDescription).HasMaxLength(160);
    }
}

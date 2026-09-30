using Ghuri.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Ops;

internal sealed class FileObjectConfiguration : IEntityTypeConfiguration<FileObject>
{
    public void Configure(EntityTypeBuilder<FileObject> builder)
    {
        builder.ToTable("FileObjects", schema: "ops");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.StorageKey).HasMaxLength(400).IsUnicode(false).IsRequired();
        builder.HasIndex(f => f.StorageKey).IsUnique();

        builder.Property(f => f.OriginalName).HasMaxLength(255).IsRequired();
        builder.Property(f => f.ContentType).HasMaxLength(100).IsUnicode(false).IsRequired();

        builder.Property(f => f.SizeBytes).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_FileObjects_SizeBytes", $"[SizeBytes] <= {FileObject.MaxSizeBytes}"));

        builder.Property(f => f.Sha256).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(f => f.IsPublic).IsRequired();
        builder.Property(f => f.UploadedBy);
        builder.Property(f => f.CreatedAtUtc).IsRequired();
    }
}

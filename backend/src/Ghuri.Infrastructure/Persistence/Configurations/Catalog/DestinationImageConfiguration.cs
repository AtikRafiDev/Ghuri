using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class DestinationImageConfiguration : IEntityTypeConfiguration<DestinationImage>
{
    public void Configure(EntityTypeBuilder<DestinationImage> builder)
    {
        builder.ToTable("DestinationImages", schema: "catalog");

        // (Id is "never generated", like every BaseEntity - see AppDbContext.)
        builder.HasKey(i => i.Id);

        builder.Property(i => i.DestinationId).IsRequired();
        builder.HasIndex(i => i.DestinationId);

        // Restrict: an uploaded file can't be deleted while a gallery uses it.
        builder.Property(i => i.FileId).IsRequired();
        builder.HasOne<FileObject>().WithMany().HasForeignKey(i => i.FileId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(i => i.SortOrder).IsRequired();
    }
}

using Ghuri.Domain.Entities.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Support;

internal sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.ToTable("ContactMessages", schema: "support");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).HasMaxLength(150).IsRequired();
        builder.Property(m => m.Email).HasMaxLength(256);
        builder.Property(m => m.Phone).HasMaxLength(20).IsUnicode(false);
        builder.Property(m => m.Subject).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Message).HasMaxLength(4000).IsRequired();

        builder.Property(m => m.Status).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_ContactMessages_Status", "[Status] IN (1,2,3)"));

        builder.Property(m => m.CreatedAtUtc).IsRequired();
    }
}

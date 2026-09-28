using Ghuri.Domain.Entities.Notify;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Notify;

internal sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("NotificationTemplates", schema: "notify");

        builder.HasKey(t => t.Id);
        builder.HasAuditColumns();

        builder.Property(t => t.Code).HasMaxLength(60).IsUnicode(false).IsRequired();

        builder.Property(t => t.Channel).HasConversion<byte>().IsRequired();
        builder.ToTable(tb => tb.HasCheckConstraint("CK_NotificationTemplates_Channel", "[Channel] IN (1,2,3)"));

        builder.Property(t => t.Language).HasMaxLength(2).IsFixedLength().IsUnicode(false).IsRequired();

        builder.HasIndex(t => new { t.Code, t.Channel, t.Language }).IsUnique();

        builder.Property(t => t.Subject).HasMaxLength(200);
        builder.Property(t => t.Body).IsRequired(); // NVARCHAR(MAX)
        builder.Property(t => t.IsActive).IsRequired();
    }
}

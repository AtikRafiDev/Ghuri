using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Entities.Notify;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Notify;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications", schema: "notify");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).UseIdentityColumn();

        builder.Property(n => n.UserId);
        builder.HasOne<User>().WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(n => new { n.UserId, n.IsRead });

        builder.Property(n => n.Channel).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Notifications_Channel", "[Channel] IN (1,2,3)"));

        builder.Property(n => n.Destination).HasMaxLength(256).IsUnicode(false).IsRequired();
        builder.Property(n => n.TemplateCode).HasMaxLength(60).IsUnicode(false).IsRequired();
        builder.Property(n => n.Subject).HasMaxLength(200);
        builder.Property(n => n.Body).IsRequired(); // NVARCHAR(MAX)

        builder.Property(n => n.Status).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Notifications_Status", "[Status] IN (1,2,3)"));
        builder.HasIndex(n => new { n.Status, n.NextAttemptAtUtc });

        builder.Property(n => n.Attempts).IsRequired();
        builder.Property(n => n.NextAttemptAtUtc);
        builder.Property(n => n.SentAtUtc);
        builder.Property(n => n.Error).HasMaxLength(500);
        builder.Property(n => n.IsRead).IsRequired();
        builder.Property(n => n.CreatedAtUtc).IsRequired();
    }
}

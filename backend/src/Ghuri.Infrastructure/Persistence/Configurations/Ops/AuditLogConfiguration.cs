using Ghuri.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Ops;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", schema: "ops");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).UseIdentityColumn();

        builder.Property(a => a.UserId);
        builder.HasIndex(a => a.UserId);

        builder.Property(a => a.Action).HasMaxLength(50).IsUnicode(false).IsRequired();

        builder.Property(a => a.EntityName).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.HasIndex(a => new { a.EntityName, a.EntityId });

        builder.Property(a => a.ChangesJson); // NVARCHAR(MAX), nullable
        builder.Property(a => a.IpAddress).HasMaxLength(45).IsUnicode(false);
        builder.Property(a => a.UserAgent).HasMaxLength(300);
        builder.Property(a => a.AtUtc).IsRequired();
    }
}

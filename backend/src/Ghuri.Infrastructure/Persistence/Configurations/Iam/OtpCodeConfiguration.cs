using Ghuri.Domain.Entities.Iam;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Iam;

internal sealed class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.ToTable("OtpCodes", schema: "iam");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).UseIdentityColumn();

        builder.Property(o => o.Destination).HasMaxLength(256).IsUnicode(false).IsRequired();
        builder.HasIndex(o => new { o.Destination, o.CreatedAtUtc });

        builder.Property(o => o.Purpose).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_OtpCodes_Purpose", "[Purpose] IN (1,2,3)"));

        builder.Property(o => o.CodeHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();

        builder.Property(o => o.ExpiresAtUtc).IsRequired();

        builder.Property(o => o.Attempts).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_OtpCodes_Attempts", "[Attempts] <= 5"));

        builder.Property(o => o.ConsumedAtUtc);
        builder.Property(o => o.CreatedAtUtc).IsRequired();
    }
}

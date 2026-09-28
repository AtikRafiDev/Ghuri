using Ghuri.Domain.Entities.Iam;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Iam;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", schema: "iam");

        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).UseIdentityColumn(); // BIGINT IDENTITY, not app-generated

        builder.Property(rt => rt.UserId).IsRequired();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade); // deleting a user removes their sessions
        builder.HasIndex(rt => rt.UserId);

        builder.Property(rt => rt.TokenHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.HasIndex(rt => rt.TokenHash).IsUnique();

        builder.Property(rt => rt.FamilyId).IsRequired();
        builder.HasIndex(rt => rt.FamilyId);

        builder.Property(rt => rt.ExpiresAtUtc).IsRequired();
        builder.Property(rt => rt.CreatedAtUtc).IsRequired();
        builder.Property(rt => rt.RevokedAtUtc);

        builder.Property(rt => rt.ReplacedByHash).HasMaxLength(64).IsFixedLength().IsUnicode(false);

        builder.Property(rt => rt.CreatedByIp).HasMaxLength(45).IsUnicode(false);
        builder.Property(rt => rt.UserAgent).HasMaxLength(300);
    }
}

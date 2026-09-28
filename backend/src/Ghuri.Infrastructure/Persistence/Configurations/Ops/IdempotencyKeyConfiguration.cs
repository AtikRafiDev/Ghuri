using Ghuri.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Ops;

internal sealed class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("IdempotencyKeys", schema: "ops");

        // A STRING primary key, not a Guid - the client's own
        // Idempotency-Key header value IS the key, so it needs an
        // explicit max length (a primary key column can't be MAX-sized).
        builder.HasKey(k => k.Key);
        builder.Property(k => k.Key).HasMaxLength(80).IsUnicode(false);

        builder.Property(k => k.UserId);
        builder.Property(k => k.RequestHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(k => k.StatusCode).IsRequired();
        builder.Property(k => k.ResponseJson).IsRequired(); // NVARCHAR(MAX)
        builder.Property(k => k.CreatedAtUtc).IsRequired();

        builder.Property(k => k.ExpiresAtUtc).IsRequired();
        builder.HasIndex(k => k.ExpiresAtUtc); // the Housekeeping job's "purge after 24h" query
    }
}

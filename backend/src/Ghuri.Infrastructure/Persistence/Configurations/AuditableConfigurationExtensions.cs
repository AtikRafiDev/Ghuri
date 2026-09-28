using Ghuri.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations;

/// <summary>
/// Shared helper so every [A] entity's configuration doesn't repeat the
/// same four lines.
/// </summary>
internal static class AuditableConfigurationExtensions
{
    /// <summary>
    /// Adds the blueprint's [A] audit columns (CreatedAtUtc, CreatedBy,
    /// UpdatedAtUtc, UpdatedBy) as EF Core "shadow properties" - real
    /// columns in the database with no matching C# property on the entity
    /// (see IAuditable's comment for why). A SaveChanges interceptor,
    /// built later in Infrastructure, fills them in automatically on
    /// every insert/update - nothing else needs to touch them.
    /// </summary>
    public static void HasAuditColumns<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IAuditable
    {
        builder.Property<DateTime>("CreatedAtUtc").IsRequired();
        builder.Property<Guid?>("CreatedBy");
        builder.Property<DateTime?>("UpdatedAtUtc");
        builder.Property<Guid?>("UpdatedBy");
    }
}

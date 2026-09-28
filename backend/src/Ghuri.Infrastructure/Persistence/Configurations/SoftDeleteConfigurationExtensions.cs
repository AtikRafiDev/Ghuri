using Ghuri.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations;

internal static class SoftDeleteConfigurationExtensions
{
    /// <summary>
    /// Adds a global query filter so every normal query (Where, FirstOrDefault,
    /// the read side's IReadDbContext...) automatically skips soft-deleted
    /// rows - nobody has to remember to add "WHERE IsDeleted = 0" themselves.
    /// </summary>
    public static void HasSoftDeleteFilter<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, ISoftDeletable
    {
        builder.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

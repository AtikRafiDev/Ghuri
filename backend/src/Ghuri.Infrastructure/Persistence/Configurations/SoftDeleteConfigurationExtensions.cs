using Ghuri.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations;

internal static class SoftDeleteConfigurationExtensions
{
    /// <summary>
    /// For unique indexes on soft-deletable tables: .HasFilter(NotDeleted)
    /// makes the index ignore deleted rows (a SQL Server "filtered index"),
    /// so deleting "Sylhet" frees its name/slug for a new "Sylhet".
    /// Without it, the hidden row would still block the name, and the
    /// duplicate check in the handler - which can't see deleted rows -
    /// would pass and then crash on the index.
    /// </summary>
    public const string NotDeleted = "[IsDeleted] = 0";

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

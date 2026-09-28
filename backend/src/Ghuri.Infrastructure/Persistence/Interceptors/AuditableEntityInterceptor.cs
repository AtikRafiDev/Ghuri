using System.Text.Json;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Domain.Common;
using Ghuri.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Ghuri.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Runs automatically right before EVERY SaveChanges (blueprint: "Audit
/// interceptor - fills CreatedBy/UpdatedBy and writes AuditLogs
/// automatically"). No handler ever sets these by hand, so none can forget.
/// </summary>
/// <remarks>
/// For every [A] (IAuditable) entity being saved it:
/// 1. Fills the 4 invisible audit columns (CreatedAtUtc/CreatedBy on
///    insert, UpdatedAtUtc/UpdatedBy on update).
/// 2. Adds one ops.AuditLogs row describing what changed - saved in the
///    SAME SaveChanges, so the data change and its audit record can never
///    get out of sync.
/// </remarks>
internal sealed class AuditableEntityInterceptor(ICurrentUser currentUser, TimeProvider clock) : SaveChangesInterceptor
{
    // Never written into AuditLogs.ChangesJson in plain text - blueprint:
    // "old / new values (sensitive fields masked)". A stolen audit log must
    // not become a way to steal passwords or passports.
    private static readonly HashSet<string> SensitiveProperties =
        ["PasswordHash", "SecurityStamp", "TokenHash", "ReplacedByHash", "CodeHash", "PassportNo", "RequestHash"];

    // Bookkeeping columns - recording "UpdatedAtUtc changed" in every log
    // row would be pure noise.
    private static readonly HashSet<string> IgnoredProperties =
        ["CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy", "RowVersion"];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            Apply(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            Apply(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext context)
    {
        // TimeProvider instead of DateTime.UtcNow: a test can plug in a
        // fixed clock and check the exact timestamp written.
        var now = clock.GetUtcNow().UtcDateTime;
        var userId = currentUser.UserId;
        var auditLogs = new List<AuditLog>();

        // ToList() first: the loop adds AuditLog entities to the same
        // context, and changing a collection while looping over it throws.
        foreach (var entry in context.ChangeTracker.Entries<IAuditable>().ToList())
        {
            string action;
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property("CreatedAtUtc").CurrentValue = now;
                    entry.Property("CreatedBy").CurrentValue = userId;
                    action = "Created";
                    break;

                case EntityState.Modified:
                    entry.Property("UpdatedAtUtc").CurrentValue = now;
                    entry.Property("UpdatedBy").CurrentValue = userId;
                    // Whatever happened in memory, an UPDATE must never
                    // rewrite when/by whom the row was originally created.
                    entry.Property("CreatedAtUtc").IsModified = false;
                    entry.Property("CreatedBy").IsModified = false;
                    action = IsBeingSoftDeleted(entry) ? "Deleted" : "Updated";
                    break;

                case EntityState.Deleted:
                    action = "Deleted";
                    break;

                default:
                    continue; // Unchanged / Detached - nothing to record
            }

            var changes = DescribeChanges(entry);
            if (entry.State == EntityState.Modified && changes is null)
                continue; // marked Modified but no real column changed - nothing worth logging

            auditLogs.Add(AuditLog.Record(
                action,
                entityName: entry.Metadata.ClrType.Name,
                entityId: entry.Property("Id").CurrentValue?.ToString() ?? string.Empty,
                nowUtc: now,
                userId: userId,
                changesJson: changes));
        }

        if (auditLogs.Count > 0)
            context.Set<AuditLog>().AddRange(auditLogs);
    }

    /// <summary>Soft delete is an UPDATE that flips IsDeleted false -> true; log it as a delete, which is what it means.</summary>
    private static bool IsBeingSoftDeleted(EntityEntry entry) =>
        entry.Entity is ISoftDeletable
        && entry.Property(nameof(ISoftDeletable.IsDeleted)) is { IsModified: true, CurrentValue: true };

    /// <summary>
    /// Insert: every value. Update: only changed columns, as {Old, New}.
    /// Delete: the last known values. Sensitive columns become "***".
    /// </summary>
    private static string? DescribeChanges(EntityEntry entry)
    {
        var changes = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            if (IgnoredProperties.Contains(name) || property.Metadata.IsPrimaryKey())
                continue;
            if (entry.State == EntityState.Modified && !property.IsModified)
                continue;

            var sensitive = SensitiveProperties.Contains(name);
            object? Mask(object? value) => sensitive && value is not null ? "***" : value;

            changes[name] = entry.State switch
            {
                EntityState.Added => Mask(property.CurrentValue),
                EntityState.Deleted => Mask(property.OriginalValue),
                _ => new { Old = Mask(property.OriginalValue), New = Mask(property.CurrentValue) }
            };
        }

        return changes.Count == 0 ? null : JsonSerializer.Serialize(changes);
    }
}

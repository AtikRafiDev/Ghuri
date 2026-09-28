using Ghuri.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Infrastructure.Persistence;

/// <summary>
/// The EF Core implementation of IUnitOfWork (blueprint section 7.2).
/// One command = one transaction = one SaveChanges.
/// </summary>
internal sealed class EfUnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> work,
        Func<T, bool> shouldCommit,
        CancellationToken cancellationToken = default)
    {
        // The execution strategy is how EF Core retries on transient SQL
        // errors (a dropped connection, a deadlock). A manually-opened
        // transaction MUST run inside it, or EF Core refuses when retries
        // are enabled - so this is written the safe way from day one.
        var strategy = db.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var result = await work(cancellationToken);

            if (!shouldCommit(result))
            {
                await transaction.RollbackAsync(cancellationToken);
                // Forget every tracked change the failed handler made, so
                // nothing half-done leaks into a later SaveChanges.
                db.ChangeTracker.Clear();
                return result;
            }

            await db.SaveChangesAsync(cancellationToken); // the ONLY SaveChanges call a command ever triggers
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }
}

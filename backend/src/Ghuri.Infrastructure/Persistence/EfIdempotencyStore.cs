using Ghuri.Application.Abstractions.Data;
using Ghuri.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Infrastructure.Persistence;

/// <summary>EF Core implementation of IIdempotencyStore, on ops.IdempotencyKeys.</summary>
internal sealed class EfIdempotencyStore(AppDbContext db) : IIdempotencyStore
{
    /// <summary>How long an answer is kept for replays. A Housekeeping job will purge older rows (ExpiresAtUtc is indexed for it).</summary>
    private static readonly TimeSpan KeepFor = TimeSpan.FromHours(24);

    /// <summary>
    /// UPDLOCK + HOLDLOCK: lock this key - even if no row exists yet - until
    /// the transaction ends. A second request with the same key, arriving a
    /// millisecond later, waits HERE until the first commits; then it reads
    /// the saved answer instead of booking again. (Without the lock, both
    /// would see "nothing saved yet" and both would book.)
    /// </summary>
    public async Task<StoredResponse?> FindAndLockAsync(string key, CancellationToken cancellationToken)
    {
        // FromSqlInterpolated turns {key} into a SQL parameter - never pasted into the text.
        var rows = await db.IdempotencyKeys
            .FromSqlInterpolated($"SELECT * FROM [ops].[IdempotencyKeys] WITH (UPDLOCK, HOLDLOCK) WHERE [Key] = {key}")
            .AsNoTracking()
            .Select(k => new StoredResponse(k.UserId, k.RequestHash, k.StatusCode, k.ResponseJson))
            .ToListAsync(cancellationToken);

        return rows.SingleOrDefault();
    }

    public void Save(string key, Guid? userId, string requestHash, int statusCode, string responseJson, DateTime nowUtc) =>
        db.IdempotencyKeys.Add(IdempotencyKey.Create(key, requestHash, (short)statusCode, responseJson, nowUtc, nowUtc + KeepFor, userId));
}

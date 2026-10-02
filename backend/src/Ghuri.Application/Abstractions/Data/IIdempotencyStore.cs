namespace Ghuri.Application.Abstractions.Data;

/// <summary>
/// Remembers the answer to a POST by its Idempotency-Key header (blueprint:
/// ops.IdempotencyKeys), so a retried request - a double-click, a phone
/// that lost signal and sent it again - gets the FIRST answer back instead
/// of creating a second booking and reserving seats twice.
/// </summary>
/// <remarks>
/// Used inside a command's transaction: FindAndLockAsync first, the real
/// work, then Save. The lock makes a second, simultaneous request with the
/// same key wait until the first one commits - then it finds the saved answer.
/// </remarks>
public interface IIdempotencyStore
{
    /// <summary>The saved answer for this key, or null if it's new. Either way the key stays locked until the transaction ends.</summary>
    Task<StoredResponse?> FindAndLockAsync(string key, CancellationToken cancellationToken);

    /// <summary>Saves the answer - written by the same SaveChanges as the booking, so both are kept or neither.</summary>
    void Save(string key, Guid? userId, string requestHash, int statusCode, string responseJson, DateTime nowUtc);
}

/// <summary>A saved answer: who sent it, a fingerprint of what they sent, and what they got back.</summary>
public sealed record StoredResponse(Guid? UserId, string RequestHash, int StatusCode, string ResponseJson);

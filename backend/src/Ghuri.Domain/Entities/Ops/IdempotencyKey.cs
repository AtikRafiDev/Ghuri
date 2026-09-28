namespace Ghuri.Domain.Entities.Ops;

/// <summary>
/// A stored response for a retried POST request (blueprint:
/// ops.IdempotencyKeys). Keyed by the client-supplied Idempotency-Key
/// header value, not a generated id - a client retrying the exact same
/// request gets back the exact same response instead of creating a second
/// booking/payment/etc.
/// </summary>
public sealed class IdempotencyKey
{
    /// <summary>The Idempotency-Key header value itself - the primary key.</summary>
    public string Key { get; private set; } = string.Empty;

    public Guid? UserId { get; private set; }

    /// <summary>SHA-256 of the request body - if the same key arrives with a DIFFERENT body, that's a client bug, not a legitimate retry, and gets rejected (422).</summary>
    public string RequestHash { get; private set; } = string.Empty;

    public short StatusCode { get; private set; }
    public string ResponseJson { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }

    private IdempotencyKey()
    {
    }

    public static IdempotencyKey Create(
        string key, string requestHash, short statusCode, string responseJson, DateTime nowUtc, DateTime expiresAtUtc, Guid? userId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(responseJson);

        return new IdempotencyKey
        {
            Key = key,
            UserId = userId,
            RequestHash = requestHash,
            StatusCode = statusCode,
            ResponseJson = responseJson,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = expiresAtUtc
        };
    }
}

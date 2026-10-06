using Ghuri.Domain.Common;

namespace Ghuri.Domain.Entities.Ops;

/// <summary>
/// A domain event waiting to be dispatched (blueprint: ops.OutboxMessages).
/// Saved in the SAME transaction as the data change that raised it (the
/// "transactional outbox" pattern) - written by the SaveChanges
/// interceptor, read and processed by the OutboxDispatcher background job.
/// Neither is built yet; this is just the table's shape.
/// </summary>
public sealed class OutboxMessage : BaseEntity
{
    /// <summary>The event's .NET type name, e.g. "BookingConfirmed" - used to pick a deserializer when dispatching.</summary>
    public string Type { get; private set; } = string.Empty;

    public string PayloadJson { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }

    /// <summary>Null means still pending - the dispatcher's query filters on this being null.</summary>
    public DateTime? ProcessedAtUtc { get; private set; }

    public byte Attempts { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage()
    {
    }

    public static OutboxMessage Create(string type, string payloadJson, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);

        return new OutboxMessage
        {
            Type = type,
            PayloadJson = payloadJson,
            OccurredAtUtc = nowUtc,
            Attempts = 0
        };
    }

    /// <summary>Handled - the dispatcher won't pick it up again.</summary>
    public void MarkProcessed(DateTime nowUtc)
    {
        Attempts++;
        ProcessedAtUtc = nowUtc;
        Error = null;
    }

    /// <summary>A handler threw. Counted; the dispatcher tries again until its attempt limit, keeping the newest error for staff.</summary>
    public void RecordFailure(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        Attempts = Attempts == byte.MaxValue ? Attempts : (byte)(Attempts + 1);
        Error = error.Length <= 1000 ? error : error[..1000]; // Error column
    }
}

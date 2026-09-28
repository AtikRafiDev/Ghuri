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
}

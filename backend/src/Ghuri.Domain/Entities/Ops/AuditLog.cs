namespace Ghuri.Domain.Entities.Ops;

/// <summary>Who changed what (blueprint: ops.AuditLogs). Written automatically by the SaveChanges interceptor (Infrastructure) - never constructed directly by command handlers.</summary>
public sealed class AuditLog
{
    public long Id { get; private set; }
    public Guid? UserId { get; private set; }

    /// <summary>"Created", "Updated", "Deleted", "Login", "Approve"...</summary>
    public string Action { get; private set; } = string.Empty;

    public string EntityName { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;

    /// <summary>Old/new values as JSON - sensitive fields (passwords, tokens) masked before this is ever written.</summary>
    public string? ChangesJson { get; private set; }

    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime AtUtc { get; private set; }

    private AuditLog()
    {
    }

    public static AuditLog Record(
        string action, string entityName, string entityId, DateTime nowUtc,
        Guid? userId = null, string? changesJson = null, string? ipAddress = null, string? userAgent = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        return new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            ChangesJson = changesJson,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            AtUtc = nowUtc
        };
    }
}

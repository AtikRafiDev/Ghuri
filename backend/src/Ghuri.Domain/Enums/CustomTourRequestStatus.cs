namespace Ghuri.Domain.Enums;

/// <summary>Maps to support.CustomTourRequests.Status (TINYINT).</summary>
public enum CustomTourRequestStatus : byte
{
    New = 1,
    Contacted = 2,
    Quoted = 3,
    Converted = 4,
    Closed = 5
}

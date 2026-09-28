namespace Ghuri.Domain.Common;

/// <summary>
/// Implemented by every entity the blueprint marks [S] (soft delete).
/// "Deleting" one of these sets IsDeleted/DeletedAtUtc instead of removing
/// the row, so the data survives for audit trails and doesn't break old
/// bookings that reference it. Infrastructure adds a global EF Core query
/// filter so ordinary queries never see a deleted row anyway - callers
/// don't need to remember to filter it out themselves.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAtUtc { get; }
}

namespace Ghuri.Domain.Common;

/// <summary>
/// Base class for every entity that has its own identity (an Id) that
/// matters more than its field values - two Bookings with identical
/// fields are still two different bookings if their Ids differ. This is
/// what separates an "entity" from a "value object" (like Money or
/// PhoneNumber, which are compared by their content instead).
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; }

    protected BaseEntity()
    {
        // Guid.CreateVersion7() (new in .NET 9) makes a UUID whose first
        // bits are a timestamp, so ids created later always sort after
        // ids created earlier - unlike Guid.NewGuid(), which is
        // completely random. The blueprint calls this a "sequential
        // GUID" (section 5.1): the app still picks the id itself before
        // saving (unlike a SQL Server IDENTITY column), but new rows
        // still land at the end of the table's clustered index instead
        // of a random spot, so the index doesn't fragment as the table
        // grows.
        Id = Guid.CreateVersion7();
    }

    public override bool Equals(object? obj) =>
        obj is BaseEntity other && other.GetType() == GetType() && other.Id == Id;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(BaseEntity? left, BaseEntity? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(BaseEntity? left, BaseEntity? right) => !(left == right);
}

namespace Ghuri.Domain.Common;

/// <summary>
/// Implemented by every entity the blueprint marks [A] (audit columns:
/// CreatedAtUtc, CreatedBy, UpdatedAtUtc, UpdatedBy). Deliberately empty -
/// those four columns are NOT C# properties on the entity. "Who and when"
/// is a bookkeeping concern, not a business rule, so it's kept out of the
/// Domain model on purpose. Infrastructure adds them as EF Core "shadow
/// properties" (columns that exist in the database with no matching C#
/// property) and a SaveChanges interceptor fills them in automatically -
/// built later, alongside AppDbContext's other plumbing.
/// </summary>
public interface IAuditable;

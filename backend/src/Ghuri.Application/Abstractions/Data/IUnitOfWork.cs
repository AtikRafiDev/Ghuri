namespace Ghuri.Application.Abstractions.Data;

/// <summary>
/// "Run this work inside one database transaction." Lives in Application
/// so command handling doesn't depend on EF Core; Infrastructure's
/// EfUnitOfWork is the real implementation (blueprint section 7.2).
/// Called ONLY by TransactionBehavior - handlers never call it, and never
/// call SaveChanges themselves.
/// </summary>
public interface IUnitOfWork
{
    /// <param name="work">The handler itself.</param>
    /// <param name="shouldCommit">
    /// Decides commit vs rollback from the handler's answer - a failed
    /// Result rolls back, even though nothing was thrown. Example from the
    /// blueprint: seats get reserved, THEN the coupon turns out invalid -
    /// the seat reservation must be undone too. (TransactionBehavior makes
    /// one exception: a failure whose Error has CommitChanges = true.)
    /// </param>
    /// <param name="cancellationToken">Cancels the work if the HTTP request is aborted.</param>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> work,
        Func<T, bool> shouldCommit,
        CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Common;

/// <summary>
/// One page of a long list - the blueprint's list response shape
/// { items, page, pageSize, totalCount, totalPages } (section 11).
/// </summary>
public sealed record Paged<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>Most rows a caller may ask for at once (blueprint: "pageSize max 100").</summary>
    public const int MaxPageSize = 100;

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Converts each row after it's loaded - for work SQL can't do, like building an image URL.</summary>
    public Paged<TOut> Map<TOut>(Func<T, TOut> map) => new(Items.Select(map).ToList(), Page, PageSize, TotalCount);
}

public static class PagingExtensions
{
    /// <summary>
    /// Two SQL queries: COUNT(*) for the total, then only this page's rows
    /// (OFFSET/FETCH). The query must already be ORDERed, or pages could
    /// overlap or skip rows.
    /// </summary>
    public static async Task<Paged<T>> ToPagedAsync<T>(
        this IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new Paged<T>(items, page, pageSize, totalCount);
    }
}

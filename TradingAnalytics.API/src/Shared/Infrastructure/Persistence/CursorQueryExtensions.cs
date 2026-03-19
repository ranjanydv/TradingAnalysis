using Microsoft.EntityFrameworkCore;
using TradingAnalytics.Shared.Kernel.Entities;
using TradingAnalytics.Shared.Kernel.Pagination;

namespace TradingAnalytics.Shared.Infrastructure.Persistence;

/// <summary>
/// Provides query helpers for cursor-based pagination.
/// </summary>
public static class CursorQueryExtensions
{
    /// <summary>
    /// Materializes a cursor result for aggregate queries ordered by UUIDv7 identifier.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="afterCursor">The cursor to start after.</param>
    /// <param name="limit">The page size.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A cursor result.</returns>
    public static async Task<CursorResult<T>> ToCursorResultAsync<T>(
        this IQueryable<T> query,
        string? afterCursor,
        int limit,
        CancellationToken ct = default)
        where T : BaseEntity
    {
        ArgumentNullException.ThrowIfNull(query);

        var afterId = CursorEncoder.DecodeId(afterCursor);
        if (afterId.HasValue)
        {
            query = query.Where(entity => entity.Id.CompareTo(afterId.Value) > 0);
        }

        var items = await query
            .OrderBy(static entity => entity.Id)
            .Take(limit + 1)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        string? nextCursor = null;
        if (items.Count > limit)
        {
            items.RemoveAt(items.Count - 1);
            nextCursor = CursorEncoder.Encode(items[^1].Id);
        }

        return new CursorResult<T>(items, nextCursor);
    }
}

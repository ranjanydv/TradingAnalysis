namespace TradingAnalytics.Shared.Kernel.Pagination;

/// <summary>
/// Represents an offset-based paged result.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResult{T}"/> class.
    /// </summary>
    /// <param name="items">The page items.</param>
    /// <param name="totalCount">The total item count.</param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The page size.</param>
    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    /// <summary>
    /// Gets the page items.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Gets the total item count.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}

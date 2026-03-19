namespace TradingAnalytics.Shared.Kernel.Pagination;

/// <summary>
/// Represents a cursor-based paged result.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class CursorResult<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CursorResult{T}"/> class.
    /// </summary>
    /// <param name="items">The returned items.</param>
    /// <param name="nextCursor">The next cursor.</param>
    /// <param name="prevCursor">The previous cursor.</param>
    public CursorResult(IReadOnlyList<T> items, string? nextCursor, string? prevCursor = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        Items = items;
        NextCursor = nextCursor;
        PrevCursor = prevCursor;
    }

    /// <summary>
    /// Gets the current page items.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Gets the next cursor.
    /// </summary>
    public string? NextCursor { get; }

    /// <summary>
    /// Gets the previous cursor.
    /// </summary>
    public string? PrevCursor { get; }

    /// <summary>
    /// Gets a value indicating whether another page exists.
    /// </summary>
    public bool HasMore => NextCursor is not null;
}

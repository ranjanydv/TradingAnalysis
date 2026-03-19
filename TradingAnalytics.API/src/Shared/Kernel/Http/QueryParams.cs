using Microsoft.AspNetCore.Mvc;

namespace TradingAnalytics.Shared.Kernel.Http;

/// <summary>
/// Represents common query-string parameters for list endpoints.
/// </summary>
public sealed class QueryParams
{
    private int _limit = 20;
    private int _size = 20;

    /// <summary>
    /// Gets or sets the forward cursor.
    /// </summary>
    [FromQuery(Name = "after")]
    public string? After { get; set; }

    /// <summary>
    /// Gets or sets the backward cursor.
    /// </summary>
    [FromQuery(Name = "before")]
    public string? Before { get; set; }

    /// <summary>
    /// Gets or sets the cursor page size.
    /// </summary>
    [FromQuery(Name = "limit")]
    public int Limit
    {
        get => _limit;
        set => _limit = Math.Clamp(value, 1, 100);
    }

    /// <summary>
    /// Gets or sets the current page number for offset pagination.
    /// </summary>
    [FromQuery(Name = "page")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size for offset pagination.
    /// </summary>
    [FromQuery(Name = "size")]
    public int Size
    {
        get => _size;
        set => _size = Math.Clamp(value, 1, 100);
    }

    /// <summary>
    /// Gets or sets the sort field.
    /// </summary>
    [FromQuery(Name = "sort")]
    public string Sort { get; set; } = "createdAt";

    /// <summary>
    /// Gets or sets the sort direction.
    /// </summary>
    [FromQuery(Name = "order")]
    public SortOrder Order { get; set; } = SortOrder.Desc;

    /// <summary>
    /// Gets or sets the free-text search term.
    /// </summary>
    [FromQuery(Name = "search")]
    public string? Search { get; set; }

    /// <summary>
    /// Gets a value indicating whether cursor pagination is requested.
    /// </summary>
    public bool IsCursorPagination => After is not null || Before is not null;

    /// <summary>
    /// Gets the offset skip value for offset pagination.
    /// </summary>
    public int OffsetSkip => Math.Max(Page - 1, 0) * Size;
}

/// <summary>
/// Represents the supported sort directions.
/// </summary>
public enum SortOrder
{
    /// <summary>
    /// Ascending order.
    /// </summary>
    Asc,

    /// <summary>
    /// Descending order.
    /// </summary>
    Desc,
}

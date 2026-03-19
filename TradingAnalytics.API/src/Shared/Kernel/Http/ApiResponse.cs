namespace TradingAnalytics.Shared.Kernel.Http;

/// <summary>
/// Represents the standard API response envelope for value payloads.
/// </summary>
/// <typeparam name="T">The payload type.</typeparam>
public sealed class ApiResponse<T>
{
    private ApiResponse()
    {
    }

    /// <summary>
    /// Gets the response message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the response payload.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Gets the total item count for paged responses.
    /// </summary>
    public int? Count { get; init; }

    /// <summary>
    /// Gets the current page number for offset-based pagination.
    /// </summary>
    public int? CurrentPage { get; init; }

    /// <summary>
    /// Gets the total page count for offset-based pagination.
    /// </summary>
    public int? TotalPage { get; init; }

    /// <summary>
    /// Gets the next cursor for cursor-based pagination.
    /// </summary>
    public string? NextCursor { get; init; }

    /// <summary>
    /// Gets the previous cursor for cursor-based pagination.
    /// </summary>
    public string? PrevCursor { get; init; }

    /// <summary>
    /// Creates a successful response.
    /// </summary>
    /// <param name="message">The response message.</param>
    /// <param name="data">The response payload.</param>
    /// <returns>A successful API response.</returns>
    public static ApiResponse<T> Ok(string message, T? data = default) =>
        new() { Message = message, Data = data };

    /// <summary>
    /// Creates an offset-paged response.
    /// </summary>
    /// <param name="message">The response message.</param>
    /// <param name="data">The response payload.</param>
    /// <param name="count">The total item count.</param>
    /// <param name="page">The current page number.</param>
    /// <param name="size">The page size.</param>
    /// <returns>A paged API response.</returns>
    public static ApiResponse<T> Paged(string message, T data, int count, int page, int size) =>
        new()
        {
            Message = message,
            Data = data,
            Count = count,
            CurrentPage = page,
            TotalPage = size <= 0 ? 0 : (int)Math.Ceiling((double)count / size),
        };

    /// <summary>
    /// Creates a cursor-paged response.
    /// </summary>
    /// <param name="message">The response message.</param>
    /// <param name="data">The response payload.</param>
    /// <param name="nextCursor">The next cursor.</param>
    /// <param name="prevCursor">The previous cursor.</param>
    /// <returns>A cursor response.</returns>
    public static ApiResponse<T> Cursored(string message, T data, string? nextCursor, string? prevCursor = null) =>
        new() { Message = message, Data = data, NextCursor = nextCursor, PrevCursor = prevCursor };
}

/// <summary>
/// Represents the standard API response envelope without a payload.
/// </summary>
public sealed class ApiResponse
{
    private ApiResponse()
    {
    }

    /// <summary>
    /// Gets the response message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Creates a successful response.
    /// </summary>
    /// <param name="message">The response message.</param>
    /// <returns>A successful API response.</returns>
    public static ApiResponse Ok(string message) => new() { Message = message };
}

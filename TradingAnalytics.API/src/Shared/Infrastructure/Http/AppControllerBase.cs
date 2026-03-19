using Microsoft.AspNetCore.Mvc;
using TradingAnalytics.Shared.Kernel.Http;
using TradingAnalytics.Shared.Kernel.Pagination;
using TradingAnalytics.Shared.Kernel.Results;

namespace TradingAnalytics.Shared.Infrastructure.Http;

/// <summary>
/// Provides common API controller helpers.
/// </summary>
[ApiController]
public abstract class AppControllerBase : ControllerBase
{
    /// <summary>
    /// Returns a successful or failed result response.
    /// </summary>
    protected IActionResult OkResult<T>(Result<T> result, string message, int status = StatusCodes.Status200OK)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
        {
            return BadRequest(ApiResponse<object?>.Ok(result.Error!));
        }

        var response = ApiResponse<T>.Ok(message, result.Value);
        return status == StatusCodes.Status201Created ? StatusCode(status, response) : Ok(response);
    }

    /// <summary>
    /// Returns a created response for a successful result.
    /// </summary>
    protected IActionResult CreatedResult<T>(Result<T> result, string message) => OkResult(result, message, StatusCodes.Status201Created);

    /// <summary>
    /// Returns a successful message-only response.
    /// </summary>
    protected IActionResult OkMessage(string message) => Ok(ApiResponse.Ok(message));

    /// <summary>
    /// Returns a cursor-paged response.
    /// </summary>
    protected IActionResult CursoredResult<T>(Result<CursorResult<T>> result, string message)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
        {
            return BadRequest(ApiResponse<object?>.Ok(result.Error!));
        }

        return Ok(ApiResponse<List<T>>.Cursored(
            message,
            result.Value!.Items.ToList(),
            result.Value.NextCursor,
            result.Value.PrevCursor));
    }
}

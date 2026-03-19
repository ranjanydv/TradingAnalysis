namespace TradingAnalytics.Modules.Identity.Application.Dtos;

/// <summary>
/// Represents a customer authentication response.
/// </summary>
public sealed class AuthResponseDto
{
    /// <summary>
    /// Gets or sets the JWT access token.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional raw session token.
    /// </summary>
    public string? SessionToken { get; init; }

    /// <summary>
    /// Gets or sets the authenticated customer.
    /// </summary>
    public CustomerProfileDto? Customer { get; init; }
}

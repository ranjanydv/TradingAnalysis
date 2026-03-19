namespace TradingAnalytics.Modules.Identity.Application.Dtos;

/// <summary>
/// Represents a customer authentication response.
/// </summary>
public sealed class AuthResponseDto
{
    /// <summary>
    /// Gets or sets the issued tokens.
    /// </summary>
    public AuthTokensDto Tokens { get; init; } = new();

    /// <summary>
    /// Gets or sets the authenticated customer profile.
    /// </summary>
    public CustomerProfileDto Profile { get; init; } = new();
}

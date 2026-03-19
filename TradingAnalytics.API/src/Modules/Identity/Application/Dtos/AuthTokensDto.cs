namespace TradingAnalytics.Modules.Identity.Application.Dtos;

/// <summary>
/// Represents issued authentication tokens.
/// </summary>
public sealed class AuthTokensDto
{
    /// <summary>
    /// Gets or sets the JWT access token.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the raw refresh token.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    public Guid SessionId { get; init; }

    /// <summary>
    /// Gets or sets the access token lifetime in seconds.
    /// </summary>
    public int AccessTokenExpiresInSeconds { get; init; }

    /// <summary>
    /// Gets or sets the refresh token lifetime in seconds.
    /// </summary>
    public int RefreshTokenExpiresInSeconds { get; init; }
}

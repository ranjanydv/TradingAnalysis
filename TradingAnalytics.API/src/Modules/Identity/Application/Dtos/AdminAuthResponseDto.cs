namespace TradingAnalytics.Modules.Identity.Application.Dtos;

/// <summary>
/// Represents an admin authentication response.
/// </summary>
public sealed class AdminAuthResponseDto
{
    /// <summary>
    /// Gets or sets the issued tokens.
    /// </summary>
    public AuthTokensDto Tokens { get; init; } = new();

    /// <summary>
    /// Gets or sets the admin identifier.
    /// </summary>
    public Guid AdminId { get; init; }

    /// <summary>
    /// Gets or sets the admin email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the assigned role identifier.
    /// </summary>
    public int? RoleId { get; init; }
}

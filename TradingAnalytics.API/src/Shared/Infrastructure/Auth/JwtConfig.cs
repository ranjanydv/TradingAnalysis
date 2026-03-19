using System.ComponentModel.DataAnnotations;

namespace TradingAnalytics.Shared.Infrastructure.Auth;

/// <summary>
/// Represents JWT configuration values.
/// </summary>
public sealed class JwtConfig
{
    /// <summary>
    /// Gets or sets the signing secret.
    /// </summary>
    [Required]
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token issuer.
    /// </summary>
    [Required]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token audience.
    /// </summary>
    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token lifetime in minutes.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int ExpiryMinutes { get; set; } = 60;
}

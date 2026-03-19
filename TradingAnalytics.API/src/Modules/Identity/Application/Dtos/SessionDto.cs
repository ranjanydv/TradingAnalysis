using TradingAnalytics.Modules.Identity.Domain.Enums;

namespace TradingAnalytics.Modules.Identity.Application.Dtos;

/// <summary>
/// Represents a session summary.
/// </summary>
public sealed class SessionDto
{
    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the raw session token.
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation timestamp in UTC.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets the expiration timestamp in UTC.
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Gets or sets the session type.
    /// </summary>
    public SessionType Type { get; init; }

    /// <summary>
    /// Gets or sets the optional linked user-device identifier.
    /// </summary>
    public Guid? UserDeviceId { get; init; }

    /// <summary>
    /// Gets or sets the originating IP address.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// Gets or sets the user agent.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the session is expired.
    /// </summary>
    public bool IsExpired { get; init; }
}

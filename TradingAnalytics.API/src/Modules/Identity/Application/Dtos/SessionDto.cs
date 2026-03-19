namespace TradingAnalytics.Modules.Identity.Application.Dtos;

/// <summary>
/// Represents a session summary.
/// </summary>
public sealed class SessionDto
{
    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    public Guid SessionId { get; init; }

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
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the device display name.
    /// </summary>
    public string? DeviceName { get; init; }

    /// <summary>
    /// Gets or sets the device type.
    /// </summary>
    public string? DeviceType { get; init; }

    /// <summary>
    /// Gets or sets the originating IP address.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the current session.
    /// </summary>
    public bool IsCurrentSession { get; init; }
}

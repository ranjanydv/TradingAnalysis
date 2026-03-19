using System.Security.Cryptography;
using TradingAnalytics.Modules.Identity.Domain.Enums;

namespace TradingAnalytics.Modules.Identity.Domain.Entities;

/// <summary>
/// Represents a persisted admin session.
/// </summary>
public sealed class AdminSession
{
    private AdminSession()
    {
    }

    /// <summary>
    /// Gets the session identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the admin identifier.
    /// </summary>
    public Guid AdminId { get; private set; }

    /// <summary>
    /// Gets the raw session token.
    /// </summary>
    public string Token { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the expiration timestamp in UTC.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Gets the session type.
    /// </summary>
    public SessionType Type { get; private set; }

    /// <summary>
    /// Gets the originating IP address.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// Gets the user agent.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Gets the creation timestamp in UTC.
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets a value indicating whether the session is expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Creates an admin session.
    /// </summary>
    /// <param name="adminId">The admin identifier.</param>
    /// <param name="type">The session type.</param>
    /// <param name="ipAddress">The optional IP address.</param>
    /// <param name="userAgent">The optional user agent.</param>
    /// <returns>The created session and raw token.</returns>
    public static (AdminSession Session, string RawToken) Create(Guid adminId, SessionType type, string? ipAddress = null, string? userAgent = null)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return (new AdminSession
        {
            AdminId = adminId,
            Token = rawToken,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            Type = type,
            IpAddress = ipAddress,
            UserAgent = userAgent,
        }, rawToken);
    }
}

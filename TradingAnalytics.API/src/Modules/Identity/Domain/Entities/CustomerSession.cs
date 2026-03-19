using TradingAnalytics.Modules.Identity.Domain.Enums;
using TradingAnalytics.Shared.Infrastructure.Auth;
using TradingAnalytics.Shared.Kernel;

namespace TradingAnalytics.Modules.Identity.Domain.Entities;

/// <summary>
/// Represents a persisted customer session.
/// </summary>
public sealed class CustomerSession
{
    private CustomerSession()
    {
    }

    /// <summary>
    /// Gets the session identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the customer identifier.
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// Gets the hashed refresh token.
    /// </summary>
    public string RefreshTokenHash { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional access token JTI.
    /// </summary>
    public string? AccessTokenJti { get; private set; }

    /// <summary>
    /// Gets the expiration timestamp in UTC.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Gets the session type.
    /// </summary>
    public SessionType Type { get; private set; }

    /// <summary>
    /// Gets the optional linked device identifier.
    /// </summary>
    public Guid? UserDeviceId { get; private set; }

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
    /// Gets the last update timestamp in UTC.
    /// </summary>
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets a value indicating whether the session is expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Creates a customer session.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="type">The session type.</param>
    /// <param name="deviceId">The optional device identifier.</param>
    /// <param name="ipAddress">The optional IP address.</param>
    /// <param name="userAgent">The optional user agent.</param>
    /// <returns>The created session and raw refresh token.</returns>
    public static (CustomerSession Session, string RawRefreshToken) Create(Guid customerId, SessionType type, Guid? deviceId = null, string? ipAddress = null, string? userAgent = null)
    {
        var (rawToken, tokenHash) = RefreshTokenHasher.Generate();
        var expiry = type == SessionType.Mobile ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddDays(7);
        return (new CustomerSession
        {
            Id = NewId.Next(),
            CustomerId = customerId,
            RefreshTokenHash = tokenHash,
            ExpiresAt = expiry,
            Type = type,
            UserDeviceId = deviceId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
        }, rawToken);
    }
}

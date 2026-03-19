using System.Security.Cryptography;
using TradingAnalytics.Modules.Identity.Domain.Enums;

namespace TradingAnalytics.Modules.Identity.Domain.Entities;

/// <summary>
/// Represents a persisted customer session.
/// </summary>
public sealed class CustomerSession
{
    private CustomerSession()
    {
    }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public SessionType Type { get; private set; }
    public Guid? UserDeviceId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public static (CustomerSession Session, string RawToken) Create(Guid customerId, SessionType type, Guid? deviceId = null, string? ipAddress = null, string? userAgent = null)
    {
        var rawToken = CreateToken();
        var expiry = type == SessionType.Mobile ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddDays(7);
        return (new CustomerSession
        {
            CustomerId = customerId,
            Token = rawToken,
            ExpiresAt = expiry,
            Type = type,
            UserDeviceId = deviceId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
        }, rawToken);
    }

    private static string CreateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}

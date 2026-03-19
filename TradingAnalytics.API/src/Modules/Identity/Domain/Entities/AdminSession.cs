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

    public Guid Id { get; private set; }
    public Guid AdminId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public SessionType Type { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

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

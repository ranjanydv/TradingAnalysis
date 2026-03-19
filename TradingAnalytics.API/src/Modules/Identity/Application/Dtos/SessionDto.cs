using TradingAnalytics.Modules.Identity.Domain.Enums;

namespace TradingAnalytics.Modules.Identity.Application.Dtos;

/// <summary>
/// Represents a session summary.
/// </summary>
public sealed class SessionDto
{
    public Guid Id { get; init; }
    public string Token { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public SessionType Type { get; init; }
    public Guid? UserDeviceId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public bool IsExpired { get; init; }
}

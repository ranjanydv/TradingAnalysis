using TradingAnalytics.Modules.Identity.Domain.Enums;
using TradingAnalytics.Shared.Kernel.Http;

namespace TradingAnalytics.Modules.Identity.Application.Dtos;

/// <summary>
/// Represents a customer device.
/// </summary>
public sealed class DeviceDto : TimestampedDto
{
    public Guid Id { get; init; }
    public string DeviceId { get; init; } = string.Empty;
    public string? FcmToken { get; init; }
    public DeviceType DeviceType { get; init; }
    public string? DeviceName { get; init; }
    public bool IsActive { get; init; }
    public DateTime LastActiveAt { get; init; }
}

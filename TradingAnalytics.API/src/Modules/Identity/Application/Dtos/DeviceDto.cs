using TradingAnalytics.Modules.Identity.Domain.Enums;
using TradingAnalytics.Shared.Kernel.Http;

namespace TradingAnalytics.Modules.Identity.Application.Dtos;

/// <summary>
/// Represents a customer device.
/// </summary>
public sealed class DeviceDto : TimestampedDto
{
    /// <summary>
    /// Gets or sets the device identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the external device id.
    /// </summary>
    public string DeviceId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the FCM token.
    /// </summary>
    public string? FcmToken { get; init; }

    /// <summary>
    /// Gets or sets the device platform.
    /// </summary>
    public DeviceType DeviceType { get; init; }

    /// <summary>
    /// Gets or sets the device name.
    /// </summary>
    public string? DeviceName { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the device is active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Gets or sets the last-activity timestamp in UTC.
    /// </summary>
    public DateTime LastActiveAt { get; init; }
}

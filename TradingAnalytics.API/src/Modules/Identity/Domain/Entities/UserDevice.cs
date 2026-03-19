using TradingAnalytics.Modules.Identity.Domain.Enums;
using TradingAnalytics.Shared.Kernel.Entities;

namespace TradingAnalytics.Modules.Identity.Domain.Entities;

/// <summary>
/// Represents a customer device registered for notifications.
/// </summary>
public sealed class UserDevice : AggregateRoot
{
    private UserDevice()
    {
    }

    /// <summary>
    /// Gets the customer identifier.
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// Gets the external device identifier.
    /// </summary>
    public string DeviceId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the FCM token.
    /// </summary>
    public string? FcmToken { get; private set; }

    /// <summary>
    /// Gets the device platform.
    /// </summary>
    public DeviceType DeviceType { get; private set; }

    /// <summary>
    /// Gets the device name.
    /// </summary>
    public string? DeviceName { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the device is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Gets the last-activity timestamp in UTC.
    /// </summary>
    public DateTime LastActiveAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Registers a new device.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="deviceId">The external device identifier.</param>
    /// <param name="deviceType">The device platform.</param>
    /// <param name="deviceName">The optional device name.</param>
    /// <param name="fcmToken">The optional FCM token.</param>
    /// <returns>The created device.</returns>
    public static UserDevice Register(Guid customerId, string deviceId, DeviceType deviceType, string? deviceName = null, string? fcmToken = null) =>
        new()
        {
            CustomerId = customerId,
            DeviceId = deviceId.Trim(),
            DeviceType = deviceType,
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName.Trim(),
            FcmToken = string.IsNullOrWhiteSpace(fcmToken) ? null : fcmToken.Trim(),
        };

    /// <summary>
    /// Updates the FCM token.
    /// </summary>
    /// <param name="token">The new token.</param>
    public void UpdateFcmToken(string? token)
    {
        FcmToken = string.IsNullOrWhiteSpace(token) ? null : token.Trim();
        Touch();
    }

    /// <summary>
    /// Records device activity.
    /// </summary>
    public void RecordActivity()
    {
        LastActiveAt = DateTime.UtcNow;
        IsActive = true;
        Touch();
    }

    /// <summary>
    /// Deactivates the device.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }
}

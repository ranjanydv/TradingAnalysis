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

    public Guid CustomerId { get; private set; }
    public string DeviceId { get; private set; } = string.Empty;
    public string? FcmToken { get; private set; }
    public DeviceType DeviceType { get; private set; }
    public string? DeviceName { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime LastActiveAt { get; private set; } = DateTime.UtcNow;

    public static UserDevice Register(Guid customerId, string deviceId, DeviceType deviceType, string? deviceName = null, string? fcmToken = null) =>
        new()
        {
            CustomerId = customerId,
            DeviceId = deviceId.Trim(),
            DeviceType = deviceType,
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName.Trim(),
            FcmToken = string.IsNullOrWhiteSpace(fcmToken) ? null : fcmToken.Trim(),
        };

    public void UpdateFcmToken(string? token)
    {
        FcmToken = string.IsNullOrWhiteSpace(token) ? null : token.Trim();
        Touch();
    }

    public void RecordActivity()
    {
        LastActiveAt = DateTime.UtcNow;
        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }
}

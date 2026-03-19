namespace TradingAnalytics.Modules.Subscriptions.Domain.Enums;

/// <summary>
/// Defines payment processing states.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment is pending.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Payment succeeded.
    /// </summary>
    Succeeded = 2,

    /// <summary>
    /// Payment failed.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Payment was refunded.
    /// </summary>
    Refunded = 4
}

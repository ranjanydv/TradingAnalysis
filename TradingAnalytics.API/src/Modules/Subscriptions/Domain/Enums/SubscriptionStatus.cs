namespace TradingAnalytics.Modules.Subscriptions.Domain.Enums;

/// <summary>
/// Defines subscription lifecycle states.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    /// Subscription is active.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Subscription is scheduled for cancellation.
    /// </summary>
    CancelAtPeriodEnd = 2,

    /// <summary>
    /// Subscription has been cancelled.
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Subscription has expired.
    /// </summary>
    Expired = 4,

    /// <summary>
    /// Subscription has been revoked administratively.
    /// </summary>
    Revoked = 5
}

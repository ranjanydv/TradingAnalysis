namespace TradingAnalytics.Modules.Subscriptions.Domain.Enums;

/// <summary>
/// Defines recurring billing intervals.
/// </summary>
public enum BillingCycle
{
    /// <summary>
    /// One-time payment.
    /// </summary>
    OneTime = 1,

    /// <summary>
    /// Monthly recurring billing.
    /// </summary>
    Monthly = 2,

    /// <summary>
    /// Quarterly recurring billing.
    /// </summary>
    Quarterly = 3,

    /// <summary>
    /// Yearly recurring billing.
    /// </summary>
    Yearly = 4
}

namespace TradingAnalytics.Modules.Subscriptions.Domain.Enums;

/// <summary>
/// Defines how access was granted.
/// </summary>
public enum AccessSource
{
    /// <summary>
    /// Access came from a direct admin grant.
    /// </summary>
    AdminGrant = 1,

    /// <summary>
    /// Access came from a successful payment.
    /// </summary>
    Payment = 2,

    /// <summary>
    /// Access came from a promotional or complimentary grant.
    /// </summary>
    Complimentary = 3
}

using TradingAnalytics.Modules.Subscriptions.Domain.Enums;

namespace TradingAnalytics.Modules.Subscriptions.Domain.Entities;

/// <summary>
/// Represents a payment attempt or settlement for a subscription.
/// </summary>
public sealed class Payment
{
    private Payment()
    {
    }

    /// <summary>
    /// Gets the integer payment identifier.
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// Gets the customer identifier.
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// Gets the related subscription identifier.
    /// </summary>
    public Guid UserSubscriptionId { get; private set; }

    /// <summary>
    /// Gets the payment amount.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Gets the currency code.
    /// </summary>
    public string Currency { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the provider name.
    /// </summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional provider transaction identifier.
    /// </summary>
    public string? ExternalReference { get; private set; }

    /// <summary>
    /// Gets the payment status.
    /// </summary>
    public PaymentStatus Status { get; private set; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful payment record.
    /// </summary>
    public static Payment CreateSucceeded(
        Guid customerId,
        Guid userSubscriptionId,
        decimal amount,
        string currency,
        string provider,
        string? externalReference) =>
        new()
        {
            CustomerId = customerId,
            UserSubscriptionId = userSubscriptionId,
            Amount = amount,
            Currency = currency.Trim().ToUpperInvariant(),
            Provider = provider.Trim(),
            ExternalReference = string.IsNullOrWhiteSpace(externalReference) ? null : externalReference.Trim(),
            Status = PaymentStatus.Succeeded
        };
}

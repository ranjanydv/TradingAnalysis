using TradingAnalytics.Modules.Subscriptions.Domain.Enums;
using TradingAnalytics.Shared.Kernel.Entities;
using TradingAnalytics.Shared.Kernel.Results;

namespace TradingAnalytics.Modules.Subscriptions.Domain.Entities;

/// <summary>
/// Represents a purchasable price point for a subscription tier.
/// </summary>
public sealed class TierPrice : BaseEntity
{
    private TierPrice()
    {
    }

    /// <summary>
    /// Gets the owning tier identifier.
    /// </summary>
    public Guid TierId { get; private set; }

    /// <summary>
    /// Gets the ISO currency code.
    /// </summary>
    public string Currency { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the monetary amount.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Gets the billing cycle.
    /// </summary>
    public BillingCycle BillingCycle { get; private set; }

    /// <summary>
    /// Gets the optional trial period in days.
    /// </summary>
    public int? TrialDays { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the price is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Creates a new tier price.
    /// </summary>
    public static Result<TierPrice> Create(Guid tierId, string currency, decimal amount, BillingCycle billingCycle, int? trialDays)
    {
        if (tierId == Guid.Empty)
        {
            return Result<TierPrice>.Failure("Tier is required.");
        }

        currency = currency.Trim().ToUpperInvariant();
        if (currency.Length != 3)
        {
            return Result<TierPrice>.Failure("Currency must be a 3-letter ISO code.");
        }

        if (amount < 0)
        {
            return Result<TierPrice>.Failure("Amount cannot be negative.");
        }

        return Result<TierPrice>.Success(new TierPrice
        {
            TierId = tierId,
            Currency = currency,
            Amount = amount,
            BillingCycle = billingCycle,
            TrialDays = trialDays
        });
    }
}

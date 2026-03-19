using TradingAnalytics.Modules.Subscriptions.Domain.Enums;

namespace TradingAnalytics.Modules.Subscriptions.Application.Dtos;

/// <summary>
/// Represents a module summary for subscription browsing.
/// </summary>
public sealed class ModuleSummaryDto
{
    /// <summary>
    /// Gets or sets the module identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the module slug.
    /// </summary>
    public string Slug { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the module name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the module description.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Represents a tier price.
/// </summary>
public sealed class TierPriceDto
{
    /// <summary>
    /// Gets or sets the price identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the currency code.
    /// </summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Gets or sets the billing cycle.
    /// </summary>
    public BillingCycle BillingCycle { get; init; }

    /// <summary>
    /// Gets or sets the trial period in days.
    /// </summary>
    public int? TrialDays { get; init; }
}

/// <summary>
/// Represents a subscription tier with prices.
/// </summary>
public sealed class SubscriptionTierDto
{
    /// <summary>
    /// Gets or sets the tier identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the tier name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the tier description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the display order.
    /// </summary>
    public int DisplayOrder { get; init; }

    /// <summary>
    /// Gets or sets the available prices.
    /// </summary>
    public List<TierPriceDto> Prices { get; init; } = [];
}

/// <summary>
/// Represents a module detail with sellable tiers.
/// </summary>
public sealed class ModuleDetailDto
{
    /// <summary>
    /// Gets or sets the module identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the module slug.
    /// </summary>
    public string Slug { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the module name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the module description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the module tiers.
    /// </summary>
    public List<SubscriptionTierDto> Tiers { get; init; } = [];
}

/// <summary>
/// Represents a customer's current access to a module.
/// </summary>
public sealed class UserAccessDto
{
    /// <summary>
    /// Gets or sets the customer identifier.
    /// </summary>
    public Guid CustomerId { get; init; }

    /// <summary>
    /// Gets or sets the module identifier.
    /// </summary>
    public Guid ModuleId { get; init; }

    /// <summary>
    /// Gets or sets the active subscription identifier.
    /// </summary>
    public Guid? SubscriptionId { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether access is active.
    /// </summary>
    public bool HasAccess { get; init; }

    /// <summary>
    /// Gets or sets the active tier identifier.
    /// </summary>
    public Guid? TierId { get; init; }

    /// <summary>
    /// Gets or sets the UTC access end timestamp.
    /// </summary>
    public DateTime? EndsAt { get; init; }

    /// <summary>
    /// Gets or sets the grant source.
    /// </summary>
    public AccessSource? Source { get; init; }
}

/// <summary>
/// Represents a user subscription summary.
/// </summary>
public sealed class UserSubscriptionDto
{
    /// <summary>
    /// Gets or sets the subscription identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the module name.
    /// </summary>
    public string ModuleName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the tier name.
    /// </summary>
    public string TierName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the subscription status.
    /// </summary>
    public SubscriptionStatus Status { get; init; }

    /// <summary>
    /// Gets or sets the start timestamp.
    /// </summary>
    public DateTime StartsAt { get; init; }

    /// <summary>
    /// Gets or sets the end timestamp.
    /// </summary>
    public DateTime EndsAt { get; init; }
}

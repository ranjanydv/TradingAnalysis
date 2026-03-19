using TradingAnalytics.Modules.Subscriptions.Application.Dtos;
using TradingAnalytics.Modules.Subscriptions.Domain.Entities;

namespace TradingAnalytics.Modules.Subscriptions.Application.Mappings;

/// <summary>
/// Provides subscription projection helpers.
/// </summary>
public static class SubscriptionMappings
{
    /// <summary>
    /// Converts a module to a summary DTO.
    /// </summary>
    public static ModuleSummaryDto ToSummaryDto(this AccessModule module) =>
        new()
        {
            Id = module.Id,
            Slug = module.Slug,
            Name = module.Name,
            Description = module.Description
        };

    /// <summary>
    /// Converts a price to a DTO.
    /// </summary>
    public static TierPriceDto ToDto(this TierPrice price) =>
        new()
        {
            Id = price.Id,
            Currency = price.Currency,
            Amount = price.Amount,
            BillingCycle = price.BillingCycle,
            TrialDays = price.TrialDays
        };

    /// <summary>
    /// Converts a tier to a DTO.
    /// </summary>
    public static SubscriptionTierDto ToDto(this SubscriptionTier tier) =>
        new()
        {
            Id = tier.Id,
            Name = tier.Name,
            Description = tier.Description,
            DisplayOrder = tier.DisplayOrder,
            Prices = tier.Prices.Where(static x => x.IsActive).OrderBy(static x => x.Amount).Select(static x => x.ToDto()).ToList()
        };
}

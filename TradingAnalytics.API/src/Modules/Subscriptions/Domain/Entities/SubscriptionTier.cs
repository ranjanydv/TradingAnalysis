using TradingAnalytics.Shared.Kernel.Entities;
using TradingAnalytics.Shared.Kernel.Results;

namespace TradingAnalytics.Modules.Subscriptions.Domain.Entities;

/// <summary>
/// Represents a sellable tier within a module.
/// </summary>
public sealed class SubscriptionTier : AggregateRoot
{
    private readonly List<TierPrice> _prices = [];

    private SubscriptionTier()
    {
    }

    /// <summary>
    /// Gets the owning module identifier.
    /// </summary>
    public Guid ModuleId { get; private set; }

    /// <summary>
    /// Gets the tier name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the display order.
    /// </summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the tier is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Gets the configured prices.
    /// </summary>
    public IReadOnlyCollection<TierPrice> Prices => _prices.AsReadOnly();

    /// <summary>
    /// Creates a new subscription tier.
    /// </summary>
    public static Result<SubscriptionTier> Create(Guid moduleId, string name, string? description, int displayOrder)
    {
        if (moduleId == Guid.Empty)
        {
            return Result<SubscriptionTier>.Failure("Module is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<SubscriptionTier>.Failure("Tier name is required.");
        }

        return Result<SubscriptionTier>.Success(new SubscriptionTier
        {
            ModuleId = moduleId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            DisplayOrder = displayOrder
        });
    }
}

using TradingAnalytics.Shared.Kernel.Entities;
using TradingAnalytics.Shared.Kernel.Results;

namespace TradingAnalytics.Modules.Subscriptions.Domain.Entities;

/// <summary>
/// Represents a product module that can be subscribed to.
/// </summary>
public sealed class AccessModule : AggregateRoot
{
    private readonly List<SubscriptionTier> _tiers = [];

    private AccessModule()
    {
    }

    /// <summary>
    /// Gets the stable module slug.
    /// </summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the module display name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the module is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Gets the configured subscription tiers.
    /// </summary>
    public IReadOnlyCollection<SubscriptionTier> Tiers => _tiers.AsReadOnly();

    /// <summary>
    /// Creates a new access module.
    /// </summary>
    public static Result<AccessModule> Create(string slug, string name, string? description)
    {
        slug = slug.Trim().ToLowerInvariant();
        name = name.Trim();

        if (string.IsNullOrWhiteSpace(slug))
        {
            return Result<AccessModule>.Failure("Module slug is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<AccessModule>.Failure("Module name is required.");
        }

        return Result<AccessModule>.Success(new AccessModule
        {
            Slug = slug,
            Name = name,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        });
    }
}

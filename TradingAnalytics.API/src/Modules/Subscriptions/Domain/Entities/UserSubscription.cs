using TradingAnalytics.Modules.Subscriptions.Domain.Enums;
using TradingAnalytics.Shared.Kernel.Entities;
using TradingAnalytics.Shared.Kernel.Results;

namespace TradingAnalytics.Modules.Subscriptions.Domain.Entities;

/// <summary>
/// Represents a customer's granted access to a module tier.
/// </summary>
public sealed class UserSubscription : AggregateRoot
{
    private UserSubscription()
    {
    }

    /// <summary>
    /// Gets the customer identifier.
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// Gets the module identifier.
    /// </summary>
    public Guid ModuleId { get; private set; }

    /// <summary>
    /// Gets the tier identifier.
    /// </summary>
    public Guid TierId { get; private set; }

    /// <summary>
    /// Gets the subscription status.
    /// </summary>
    public SubscriptionStatus Status { get; private set; }

    /// <summary>
    /// Gets the grant source.
    /// </summary>
    public AccessSource Source { get; private set; }

    /// <summary>
    /// Gets the UTC start timestamp.
    /// </summary>
    public DateTime StartsAt { get; private set; }

    /// <summary>
    /// Gets the UTC end timestamp.
    /// </summary>
    public DateTime EndsAt { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the subscription should be cancelled at period end.
    /// </summary>
    public bool CancelAtPeriodEnd { get; private set; }

    /// <summary>
    /// Gets the cancellation timestamp.
    /// </summary>
    public DateTime? CancelledAt { get; private set; }

    /// <summary>
    /// Gets the revocation timestamp.
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// Gets the optional administrative note.
    /// </summary>
    public string? Note { get; private set; }

    /// <summary>
    /// Creates a new active subscription.
    /// </summary>
    public static Result<UserSubscription> Create(
        Guid customerId,
        Guid moduleId,
        Guid tierId,
        AccessSource source,
        DateTime startsAt,
        DateTime endsAt,
        string? note)
    {
        if (customerId == Guid.Empty)
        {
            return Result<UserSubscription>.Failure("Customer is required.");
        }

        if (moduleId == Guid.Empty || tierId == Guid.Empty)
        {
            return Result<UserSubscription>.Failure("Module and tier are required.");
        }

        if (endsAt <= startsAt)
        {
            return Result<UserSubscription>.Failure("Subscription end date must be after the start date.");
        }

        return Result<UserSubscription>.Success(new UserSubscription
        {
            CustomerId = customerId,
            ModuleId = moduleId,
            TierId = tierId,
            Source = source,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Status = SubscriptionStatus.Active,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        });
    }

    /// <summary>
    /// Extends the subscription period.
    /// </summary>
    public Result Extend(DateTime newEndsAt, string? note)
    {
        if (Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Revoked)
        {
            return Result.Failure("Only active subscriptions can be extended.");
        }

        if (newEndsAt <= EndsAt)
        {
            return Result.Failure("New end date must be later than the current end date.");
        }

        EndsAt = newEndsAt;
        Status = SubscriptionStatus.Active;
        CancelAtPeriodEnd = false;
        Note = string.IsNullOrWhiteSpace(note) ? Note : note.Trim();
        return Result.Success();
    }

    /// <summary>
    /// Revokes the subscription immediately.
    /// </summary>
    public Result Revoke(string? note)
    {
        if (Status == SubscriptionStatus.Revoked)
        {
            return Result.Failure("Subscription is already revoked.");
        }

        Status = SubscriptionStatus.Revoked;
        RevokedAt = DateTime.UtcNow;
        CancelAtPeriodEnd = false;
        Note = string.IsNullOrWhiteSpace(note) ? Note : note.Trim();
        return Result.Success();
    }

    /// <summary>
    /// Expires the subscription if it has elapsed.
    /// </summary>
    public void Expire()
    {
        Status = SubscriptionStatus.Expired;
    }

    /// <summary>
    /// Marks the subscription for cancellation at period end.
    /// </summary>
    public void MarkCancelAtPeriodEnd()
    {
        CancelAtPeriodEnd = true;
        Status = SubscriptionStatus.CancelAtPeriodEnd;
    }

    /// <summary>
    /// Returns whether the subscription is currently active.
    /// </summary>
    public bool IsActiveAt(DateTime utcNow) =>
        (Status == SubscriptionStatus.Active || Status == SubscriptionStatus.CancelAtPeriodEnd)
        && StartsAt <= utcNow
        && EndsAt > utcNow;
}

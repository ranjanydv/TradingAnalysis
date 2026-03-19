using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingAnalytics.Modules.Subscriptions.Domain.Enums;
using TradingAnalytics.Shared.Infrastructure.Cache;
using TradingAnalytics.Shared.Infrastructure.Persistence;

namespace TradingAnalytics.Modules.Subscriptions.Infrastructure.Jobs;

/// <summary>
/// Expires subscriptions whose access windows have elapsed.
/// </summary>
public sealed class ExpireSubscriptionsJob(
    AppDbContext db,
    ICacheService cacheService,
    ILogger<ExpireSubscriptionsJob> logger)
{
    private readonly AppDbContext _db = db ?? throw new ArgumentNullException(nameof(db));
    private readonly ICacheService _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    private readonly ILogger<ExpireSubscriptionsJob> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Runs the job.
    /// </summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var subscriptions = await _db.UserSubscriptions
            .Where(x => (x.Status == SubscriptionStatus.Active || x.Status == SubscriptionStatus.CancelAtPeriodEnd) && x.EndsAt <= now)
            .ToListAsync(ct);

        foreach (var subscription in subscriptions)
        {
            subscription.Expire();
            await _cacheService.RemoveAsync(CacheKeys.UserAccess(subscription.CustomerId, subscription.ModuleId), ct);
        }

        if (subscriptions.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Expired {Count} subscriptions.", subscriptions.Count);
    }
}

/// <summary>
/// Finds subscriptions approaching their end date.
/// </summary>
public sealed class ExpiryReminderJob(AppDbContext db, ILogger<ExpiryReminderJob> logger)
{
    private readonly AppDbContext _db = db ?? throw new ArgumentNullException(nameof(db));
    private readonly ILogger<ExpiryReminderJob> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Runs the job.
    /// </summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        var threshold = DateTime.UtcNow.AddDays(3);
        var now = DateTime.UtcNow;
        var count = await _db.UserSubscriptions
            .CountAsync(x => x.Status == SubscriptionStatus.Active && x.EndsAt <= threshold && x.EndsAt > now, ct);

        _logger.LogInformation("Found {Count} subscriptions eligible for expiry reminders.", count);
    }
}

/// <summary>
/// Cancels subscriptions that reached the end of a scheduled cancellation period.
/// </summary>
public sealed class CancelAtPeriodEndJob(
    AppDbContext db,
    ICacheService cacheService,
    ILogger<CancelAtPeriodEndJob> logger)
{
    private readonly AppDbContext _db = db ?? throw new ArgumentNullException(nameof(db));
    private readonly ICacheService _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    private readonly ILogger<CancelAtPeriodEndJob> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Runs the job.
    /// </summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var subscriptions = await _db.UserSubscriptions
            .Where(x => x.Status == SubscriptionStatus.CancelAtPeriodEnd && x.EndsAt <= now)
            .ToListAsync(ct);

        foreach (var subscription in subscriptions)
        {
            subscription.Expire();
            await _cacheService.RemoveAsync(CacheKeys.UserAccess(subscription.CustomerId, subscription.ModuleId), ct);
        }

        if (subscriptions.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Processed {Count} cancel-at-period-end subscriptions.", subscriptions.Count);
    }
}

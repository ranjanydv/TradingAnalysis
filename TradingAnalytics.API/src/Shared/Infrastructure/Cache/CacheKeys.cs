namespace TradingAnalytics.Shared.Infrastructure.Cache;

/// <summary>
/// Defines centralized cache key patterns.
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// Gets the user access cache key.
    /// </summary>
    public static string UserAccess(Guid userId, Guid moduleId) => $"user_access:{userId}:{moduleId}";

    /// <summary>
    /// Gets the stock quote cache key.
    /// </summary>
    public static string StockQuote(string symbol) => $"stock:{symbol.ToUpperInvariant()}";

    /// <summary>
    /// Gets the market summary cache key.
    /// </summary>
    public static string MarketSummary() => "market:summary";

    /// <summary>
    /// Gets the market indices cache key.
    /// </summary>
    public static string MarketIndices() => "market:indices";

    /// <summary>
    /// Gets the top gainers cache key.
    /// </summary>
    public static string TopGainers() => "market:gainers";

    /// <summary>
    /// Gets the top losers cache key.
    /// </summary>
    public static string TopLosers() => "market:losers";

    /// <summary>
    /// Gets the subscription tiers cache key.
    /// </summary>
    public static string SubscriptionPlans(string moduleSlug) => $"plans:{moduleSlug}";
}

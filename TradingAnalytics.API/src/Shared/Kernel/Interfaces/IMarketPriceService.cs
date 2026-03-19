namespace TradingAnalytics.Shared.Kernel.Interfaces;

/// <summary>
/// Provides cross-module market price lookups.
/// </summary>
public interface IMarketPriceService
{
    /// <summary>
    /// Gets the current prices for the supplied symbols.
    /// </summary>
    /// <param name="symbols">The ticker symbols to resolve.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A symbol-to-price map.</returns>
    Task<IReadOnlyDictionary<string, decimal>> GetCurrentPricesAsync(
        IEnumerable<string> symbols,
        CancellationToken ct = default);
}

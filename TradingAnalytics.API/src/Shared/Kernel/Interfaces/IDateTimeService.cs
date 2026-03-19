namespace TradingAnalytics.Shared.Kernel.Interfaces;

/// <summary>
/// Provides UTC time access for testable domain and application code.
/// </summary>
public interface IDateTimeService
{
    /// <summary>
    /// Gets the current UTC timestamp.
    /// </summary>
    DateTime UtcNow { get; }
}

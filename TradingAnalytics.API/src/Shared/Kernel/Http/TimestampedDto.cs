namespace TradingAnalytics.Shared.Kernel.Http;

/// <summary>
/// Represents a DTO that exposes creation and update timestamps.
/// </summary>
public abstract class TimestampedDto
{
    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets the UTC update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; init; }
}

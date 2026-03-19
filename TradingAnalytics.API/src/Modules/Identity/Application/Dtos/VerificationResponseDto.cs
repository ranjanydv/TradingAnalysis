namespace TradingAnalytics.Modules.Identity.Application.Dtos;

/// <summary>
/// Represents a started verification flow.
/// </summary>
public sealed class VerificationResponseDto
{
    /// <summary>
    /// Gets or sets the verification identifier.
    /// </summary>
    public Guid VerificationId { get; init; }

    /// <summary>
    /// Gets or sets the optional customer identifier.
    /// </summary>
    public Guid? CustomerId { get; init; }
}

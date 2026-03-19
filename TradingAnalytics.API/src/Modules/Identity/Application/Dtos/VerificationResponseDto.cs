namespace TradingAnalytics.Modules.Identity.Application.Dtos;

/// <summary>
/// Represents a started verification flow.
/// </summary>
public sealed class VerificationResponseDto
{
    public Guid VerificationId { get; init; }
    public Guid? CustomerId { get; init; }
}

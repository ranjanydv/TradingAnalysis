namespace TradingAnalytics.Modules.Identity.Application.Dtos;

/// <summary>
/// Represents a customer list item.
/// </summary>
public sealed class CustomerSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public bool Banned { get; init; }
    public DateTime CreatedAt { get; init; }
}

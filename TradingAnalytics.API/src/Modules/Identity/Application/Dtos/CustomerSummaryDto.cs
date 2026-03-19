namespace TradingAnalytics.Modules.Identity.Application.Dtos;

/// <summary>
/// Represents a customer list item.
/// </summary>
public sealed class CustomerSummaryDto
{
    /// <summary>
    /// Gets or sets the customer identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    public string? Phone { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the customer is banned.
    /// </summary>
    public bool Banned { get; init; }

    /// <summary>
    /// Gets or sets the creation timestamp in UTC.
    /// </summary>
    public DateTime CreatedAt { get; init; }
}

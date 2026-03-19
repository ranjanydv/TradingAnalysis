using TradingAnalytics.Shared.Kernel.Http;

namespace TradingAnalytics.Modules.Identity.Application.Dtos;

/// <summary>
/// Represents the current customer profile.
/// </summary>
public class CustomerProfileDto : TimestampedDto
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
    /// Gets or sets a value indicating whether the email is verified.
    /// </summary>
    public bool EmailVerified { get; init; }

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    public string? Phone { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the phone is verified.
    /// </summary>
    public bool PhoneVerified { get; init; }

    /// <summary>
    /// Gets or sets the image URL.
    /// </summary>
    public string? Image { get; init; }

    /// <summary>
    /// Gets or sets the role identifier.
    /// </summary>
    public int? RoleId { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the customer is banned.
    /// </summary>
    public bool Banned { get; init; }

    /// <summary>
    /// Gets or sets the ban reason.
    /// </summary>
    public string? BanReason { get; init; }
}

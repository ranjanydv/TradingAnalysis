using TradingAnalytics.Shared.Kernel.Http;

namespace TradingAnalytics.Modules.Identity.Application.Dtos;

/// <summary>
/// Represents the current customer profile.
/// </summary>
public class CustomerProfileDto : TimestampedDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool EmailVerified { get; init; }
    public string? Phone { get; init; }
    public bool PhoneVerified { get; init; }
    public string? Image { get; init; }
    public int? RoleId { get; init; }
    public bool Banned { get; init; }
    public string? BanReason { get; init; }
}

namespace TradingAnalytics.Modules.Identity.Application.Dtos;

/// <summary>
/// Represents an admin authentication response.
/// </summary>
public sealed class AdminAuthResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string SessionToken { get; init; } = string.Empty;
    public Guid AdminId { get; init; }
    public string Email { get; init; } = string.Empty;
    public int? RoleId { get; init; }
}

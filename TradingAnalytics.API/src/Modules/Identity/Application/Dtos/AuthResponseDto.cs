namespace TradingAnalytics.Modules.Identity.Application.Dtos;

/// <summary>
/// Represents a customer authentication response.
/// </summary>
public sealed class AuthResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string? SessionToken { get; init; }
    public CustomerProfileDto? Customer { get; init; }
}

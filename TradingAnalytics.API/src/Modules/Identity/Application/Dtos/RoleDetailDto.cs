namespace TradingAnalytics.Modules.Identity.Application.Dtos;

/// <summary>
/// Represents a role with its assigned permissions.
/// </summary>
public sealed class RoleDetailDto : RoleDto
{
    /// <summary>
    /// Gets or sets the assigned permissions.
    /// </summary>
    public List<PermissionDto> Permissions { get; init; } = [];
}

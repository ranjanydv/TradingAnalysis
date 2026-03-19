namespace TradingAnalytics.Modules.Identity.Application.Dtos;

/// <summary>
/// Represents a role summary.
/// </summary>
public class RoleDto
{
    /// <summary>
    /// Gets or sets the role identifier.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets or sets the role name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the role is system-managed.
    /// </summary>
    public bool IsSystemRole { get; init; }
}

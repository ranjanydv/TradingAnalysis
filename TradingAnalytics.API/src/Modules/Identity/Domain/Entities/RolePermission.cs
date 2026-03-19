namespace TradingAnalytics.Modules.Identity.Domain.Entities;

/// <summary>
/// Represents a role-to-permission mapping.
/// </summary>
public sealed class RolePermission
{
    /// <summary>
    /// Gets or sets the role identifier.
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Gets or sets the permission identifier.
    /// </summary>
    public int PermissionId { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp in UTC.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last update timestamp in UTC.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

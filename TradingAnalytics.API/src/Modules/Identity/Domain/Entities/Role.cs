namespace TradingAnalytics.Modules.Identity.Domain.Entities;

/// <summary>
/// Represents an assignable role.
/// </summary>
public sealed class Role
{
    /// <summary>
    /// Gets or sets the role identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the role name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the role is system-managed.
    /// </summary>
    public bool IsSystemRole { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp in UTC.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last update timestamp in UTC.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

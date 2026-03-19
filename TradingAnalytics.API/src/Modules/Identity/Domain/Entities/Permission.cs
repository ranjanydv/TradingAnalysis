namespace TradingAnalytics.Modules.Identity.Domain.Entities;

/// <summary>
/// Represents a single permission code.
/// </summary>
public sealed class Permission
{
    /// <summary>
    /// Gets or sets the permission identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the module name.
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action name.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique permission code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp in UTC.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last update timestamp in UTC.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

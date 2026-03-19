namespace TradingAnalytics.Modules.Identity.Application.Dtos;

/// <summary>
/// Represents a permission summary.
/// </summary>
public sealed class PermissionDto
{
    /// <summary>
    /// Gets or sets the permission identifier.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets or sets the module name.
    /// </summary>
    public string Module { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the action name.
    /// </summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique code.
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description.
    /// </summary>
    public string? Description { get; init; }
}

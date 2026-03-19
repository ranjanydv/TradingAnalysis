using Microsoft.AspNetCore.Authorization;

namespace TradingAnalytics.Shared.Kernel.Auth;

/// <summary>
/// Represents an authorization requirement for a single permission code.
/// </summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the required permission code.
    /// </summary>
    public string Permission { get; } = permission;
}

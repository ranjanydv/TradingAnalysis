using Microsoft.AspNetCore.Authorization;

namespace TradingAnalytics.Shared.Kernel.Auth;

/// <summary>
/// Evaluates permission-based authorization requirements.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var hasPermission = context.User
            .FindAll(Constants.ClaimTypes.Permission)
            .Any(claim => claim.Value == requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

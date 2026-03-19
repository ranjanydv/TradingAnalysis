using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TradingAnalytics.Shared.Kernel;
using TradingAnalytics.Shared.Kernel.Interfaces;

namespace TradingAnalytics.Shared.Infrastructure.Auth;

/// <summary>
/// Resolves the current actor from the HTTP context.
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirstValue(Constants.ClaimTypes.UserId);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    /// <inheritdoc />
    public string? ActorType => User?.FindFirstValue(Constants.ClaimTypes.ActorType);

    /// <inheritdoc />
    public string? Role => User?.FindFirstValue(Constants.ClaimTypes.Role);

    /// <inheritdoc />
    public bool IsAdmin => ActorType == Constants.ActorTypes.Admin;

    /// <inheritdoc />
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
}

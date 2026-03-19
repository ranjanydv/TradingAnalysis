using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingAnalytics.Modules.Identity.Application.Commands;
using TradingAnalytics.Shared.Infrastructure.Http;
using TradingAnalytics.Shared.Kernel.Auth;

namespace TradingAnalytics.Modules.Identity.Presentation.Controllers;

/// <summary>
/// Exposes administrator authentication endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = SwaggerGroups.Admin)]
[Route("api/v1/admin/auth")]
public sealed class AdminAuthController(ISender sender) : AppControllerBase
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    /// <summary>
    /// Authenticates an administrator.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(AdminLoginCommand command, CancellationToken ct) =>
        OkResult(await _sender.Send(command, ct), "Admin login successful.");
}

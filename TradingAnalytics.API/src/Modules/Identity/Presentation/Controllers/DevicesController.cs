using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingAnalytics.Modules.Identity.Application.Commands;
using TradingAnalytics.Modules.Identity.Application.Queries;
using TradingAnalytics.Shared.Infrastructure.Http;
using TradingAnalytics.Shared.Kernel.Auth;
using TradingAnalytics.Shared.Kernel.Http;

namespace TradingAnalytics.Modules.Identity.Presentation.Controllers;

/// <summary>
/// Exposes customer device endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = SwaggerGroups.Customer)]
[Route("api/v1/devices")]
[Authorize(Policy = Policies.CustomerOnly)]
public sealed class DevicesController(ISender sender) : AppControllerBase
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    /// <summary>
    /// Registers or updates a customer device.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Register(RegisterDeviceCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.IsFailure ? BadRequest(ApiResponse<object?>.Ok(result.Error!)) : OkMessage("Device registered.");
    }

    /// <summary>
    /// Gets the current customer's devices.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> MyDevices(CancellationToken ct) =>
        OkResult(await _sender.Send(new GetMyDevicesQuery(), ct), "Devices retrieved.");
}

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
/// Exposes admin customer endpoints.
/// </summary>
[Route("api/v1/admin/customers")]
[Authorize(Policy = Policies.AdminOnly)]
public sealed class AdminCustomersController(ISender sender) : AppControllerBase
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    [HttpPost]
    public async Task<IActionResult> Create(AdminCreateCustomerCommand command, CancellationToken ct) =>
        CreatedResult(await _sender.Send(command, ct), "Customer created.");

    [HttpPost("{customerId:guid}/ban")]
    public async Task<IActionResult> Ban(Guid customerId, [FromBody] BanRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new AdminBanCustomerCommand(customerId, request.Reason), ct);
        return result.IsFailure ? BadRequest(ApiResponse<object?>.Ok(result.Error!)) : OkMessage("Customer banned.");
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        OkResult(await _sender.Send(new GetCustomerByIdQuery(id), ct), "Customer retrieved.");

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] QueryParams queryParams, CancellationToken ct) =>
        CursoredResult(await _sender.Send(new GetAllCustomersQuery(queryParams), ct), "Customers retrieved.");
}

/// <summary>
/// Represents an admin ban request.
/// </summary>
/// <param name="Reason">The ban reason.</param>
public sealed record BanRequest(string Reason);

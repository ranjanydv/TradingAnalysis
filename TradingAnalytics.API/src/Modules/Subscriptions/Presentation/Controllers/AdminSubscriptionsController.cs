using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingAnalytics.Modules.Subscriptions.Application.Commands;
using TradingAnalytics.Shared.Infrastructure.Http;
using TradingAnalytics.Shared.Kernel.Auth;
using TradingAnalytics.Shared.Kernel.Http;

namespace TradingAnalytics.Modules.Subscriptions.Presentation.Controllers;

/// <summary>
/// Exposes administrator subscription management endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = SwaggerGroups.Admin)]
[Route("api/v1/admin/subscriptions")]
[Authorize(Policy = Policies.AdminOnly)]
public sealed class AdminSubscriptionsController(ISender sender) : AppControllerBase
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    /// <summary>
    /// Creates a subscription module.
    /// </summary>
    [HttpPost("modules")]
    public async Task<IActionResult> CreateModule([FromBody] AdminCreateModuleCommand command, CancellationToken ct) =>
        CreatedResult(await _sender.Send(command, ct), "Subscription module created.");

    /// <summary>
    /// Creates a tier for a module.
    /// </summary>
    [HttpPost("tiers")]
    public async Task<IActionResult> CreateTier([FromBody] AdminCreateTierCommand command, CancellationToken ct) =>
        CreatedResult(await _sender.Send(command, ct), "Subscription tier created.");

    /// <summary>
    /// Updates the prices for a tier.
    /// </summary>
    [HttpPut("tiers/{tierId:guid}/prices")]
    public async Task<IActionResult> UpdateTierPrices(Guid tierId, [FromBody] UpdateTierPricesRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new AdminUpdateTierPricesCommand(tierId, request.Prices), ct);
        return result.IsFailure ? BadRequest(ApiResponse<object?>.Ok(result.Error!)) : OkMessage("Tier prices updated.");
    }

    /// <summary>
    /// Grants access to a customer.
    /// </summary>
    [HttpPost("grants")]
    public async Task<IActionResult> GrantAccess([FromBody] AdminGrantAccessCommand command, CancellationToken ct) =>
        CreatedResult(await _sender.Send(command, ct), "Subscription access granted.");

    /// <summary>
    /// Extends a subscription.
    /// </summary>
    [HttpPost("{subscriptionId:guid}/extend")]
    public async Task<IActionResult> Extend(Guid subscriptionId, [FromBody] ExtendSubscriptionRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new AdminExtendSubscriptionCommand(subscriptionId, request.EndsAtUtc, request.Note), ct);
        return result.IsFailure ? BadRequest(ApiResponse<object?>.Ok(result.Error!)) : OkMessage("Subscription extended.");
    }

    /// <summary>
    /// Revokes a subscription.
    /// </summary>
    [HttpPost("{subscriptionId:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid subscriptionId, [FromBody] RevokeSubscriptionRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new AdminRevokeSubscriptionCommand(subscriptionId, request.Note), ct);
        return result.IsFailure ? BadRequest(ApiResponse<object?>.Ok(result.Error!)) : OkMessage("Subscription revoked.");
    }
}

/// <summary>
/// Represents a request to replace tier prices.
/// </summary>
public sealed record UpdateTierPricesRequest(List<TierPriceInput> Prices);

/// <summary>
/// Represents a request to extend a subscription.
/// </summary>
public sealed record ExtendSubscriptionRequest(DateTime EndsAtUtc, string? Note);

/// <summary>
/// Represents a request to revoke a subscription.
/// </summary>
public sealed record RevokeSubscriptionRequest(string? Note);

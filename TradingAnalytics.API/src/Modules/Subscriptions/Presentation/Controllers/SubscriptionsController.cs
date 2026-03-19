using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingAnalytics.Modules.Subscriptions.Application.Queries;
using TradingAnalytics.Shared.Infrastructure.Http;
using TradingAnalytics.Shared.Kernel.Auth;

namespace TradingAnalytics.Modules.Subscriptions.Presentation.Controllers;

/// <summary>
/// Exposes customer-facing subscription endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = SwaggerGroups.Customer)]
[Route("api/v1/subscriptions")]
public sealed class SubscriptionsController(ISender sender) : AppControllerBase
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    /// <summary>
    /// Gets the active subscription modules.
    /// </summary>
    [HttpGet("modules")]
    public async Task<IActionResult> GetModules(CancellationToken ct) =>
        OkResult(await _sender.Send(new GetModulesQuery(), ct), "Subscription modules retrieved.");

    /// <summary>
    /// Gets the module catalog for a given slug.
    /// </summary>
    [HttpGet("modules/{slug}")]
    public async Task<IActionResult> GetModuleCatalog(string slug, CancellationToken ct) =>
        OkResult(await _sender.Send(new GetModuleCatalogQuery(slug), ct), "Subscription catalog retrieved.");

    /// <summary>
    /// Gets the current user's access for a module.
    /// </summary>
    [Authorize(Policy = Policies.CustomerOnly)]
    [HttpGet("modules/{moduleId:guid}/access")]
    public async Task<IActionResult> GetMyAccess(Guid moduleId, CancellationToken ct) =>
        OkResult(await _sender.Send(new GetMyAccessQuery(moduleId), ct), "Module access retrieved.");

    /// <summary>
    /// Gets the current user's subscriptions.
    /// </summary>
    [Authorize(Policy = Policies.CustomerOnly)]
    [HttpGet("me")]
    public async Task<IActionResult> GetMySubscriptions(CancellationToken ct) =>
        OkResult(await _sender.Send(new GetMySubscriptionsQuery(), ct), "Subscriptions retrieved.");
}

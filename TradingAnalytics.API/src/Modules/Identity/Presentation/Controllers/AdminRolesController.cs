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
/// Exposes administrator role and permission endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = SwaggerGroups.Admin)]
[Route("api/v1/admin/roles")]
[Authorize(Policy = Policies.AdminOnly)]
public sealed class AdminRolesController(ISender sender) : AppControllerBase
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    /// <summary>
    /// Gets all roles.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRoles(CancellationToken ct) =>
        OkResult(await _sender.Send(new GetRolesQuery(), ct), "Roles retrieved.");

    /// <summary>
    /// Gets a role by identifier.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRole(int id, CancellationToken ct) =>
        OkResult(await _sender.Send(new GetRoleByIdQuery(id), ct), "Role retrieved.");

    /// <summary>
    /// Gets all permissions.
    /// </summary>
    [HttpGet("~/api/v1/admin/permissions")]
    public async Task<IActionResult> GetPermissions(CancellationToken ct) =>
        OkResult(await _sender.Send(new GetPermissionsQuery(), ct), "Permissions retrieved.");

    /// <summary>
    /// Creates a role.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateRoleCommand command, CancellationToken ct) =>
        CreatedResult(await _sender.Send(command, ct), "Role created.");

    /// <summary>
    /// Updates role permissions.
    /// </summary>
    [HttpPut("{roleId:int}/permissions")]
    public async Task<IActionResult> UpdatePermissions(int roleId, [FromBody] UpdateRolePermissionsRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new UpdateRolePermissionsCommand(roleId, request.PermissionIds), ct);
        return result.IsFailure ? BadRequest(ApiResponse<object?>.Ok(result.Error!)) : OkMessage("Role permissions updated.");
    }

    /// <summary>
    /// Assigns a role to a customer.
    /// </summary>
    [HttpPost("~/api/v1/admin/customers/{customerId:guid}/role")]
    public async Task<IActionResult> AssignRoleToCustomer(Guid customerId, [FromBody] AssignRoleRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new AssignRoleToCustomerCommand(customerId, request.RoleId), ct);
        return result.IsFailure ? BadRequest(ApiResponse<object?>.Ok(result.Error!)) : OkMessage("Role assigned to customer.");
    }

    /// <summary>
    /// Assigns a role to an administrator.
    /// </summary>
    [HttpPost("~/api/v1/admin/admin-users/{adminId:guid}/role")]
    public async Task<IActionResult> AssignRoleToAdmin(Guid adminId, [FromBody] AssignRoleRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new AssignRoleToAdminCommand(adminId, request.RoleId), ct);
        return result.IsFailure ? BadRequest(ApiResponse<object?>.Ok(result.Error!)) : OkMessage("Role assigned to admin.");
    }

    /// <summary>
    /// Deletes a role.
    /// </summary>
    [HttpDelete("{roleId:int}")]
    public async Task<IActionResult> Delete(int roleId, CancellationToken ct)
    {
        var result = await _sender.Send(new DeleteRoleCommand(roleId), ct);
        return result.IsFailure ? BadRequest(ApiResponse<object?>.Ok(result.Error!)) : OkMessage("Role deleted.");
    }
}

/// <summary>
/// Represents a role-assignment request.
/// </summary>
public sealed record AssignRoleRequest(int RoleId);

/// <summary>
/// Represents a request to replace role permissions.
/// </summary>
public sealed record UpdateRolePermissionsRequest(List<int> PermissionIds);

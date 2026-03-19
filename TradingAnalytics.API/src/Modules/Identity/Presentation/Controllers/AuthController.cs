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
/// Exposes customer and authentication endpoints.
/// </summary>
[Route("api/v1/auth")]
public sealed class AuthController(ISender sender) : AppControllerBase
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    [HttpPost("register/email")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterWithEmail(RegisterWithEmailCommand command, CancellationToken ct) =>
        CreatedResult(await _sender.Send(command, ct), "Customer registered.");

    [HttpPost("register/phone")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterWithPhone(RegisterWithPhoneCommand command, CancellationToken ct) =>
        CreatedResult(await _sender.Send(command, ct), "Phone registration started.");

    [HttpPost("register/phone/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPhoneRegistration(VerifyPhoneRegistrationCommand command, CancellationToken ct) =>
        OkResult(await _sender.Send(command, ct), "Phone registration verified.");

    [HttpPost("login/email")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithEmail(LoginWithEmailCommand command, CancellationToken ct) =>
        OkResult(await _sender.Send(command, ct), "Login successful.");

    [HttpPost("login/phone")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithPhone(LoginWithPhoneCommand command, CancellationToken ct) =>
        OkResult(await _sender.Send(command, ct), "OTP sent.");

    [HttpPost("login/phone/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtpLogin(VerifyOtpLoginCommand command, CancellationToken ct) =>
        OkResult(await _sender.Send(command, ct), "Login successful.");

    [HttpPost("login/admin")]
    [AllowAnonymous]
    public async Task<IActionResult> AdminLogin(AdminLoginCommand command, CancellationToken ct) =>
        OkResult(await _sender.Send(command, ct), "Admin login successful.");

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(LogoutCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.IsFailure ? BadRequest(ApiResponse<object?>.Ok(result.Error!)) : OkMessage("Logged out.");
    }

    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        var result = await _sender.Send(new LogoutAllCommand(), ct);
        return result.IsFailure ? BadRequest(ApiResponse<object?>.Ok(result.Error!)) : OkMessage("Logged out from all sessions.");
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshTokenCommand command, CancellationToken ct) =>
        OkResult(await _sender.Send(command, ct), "Token refreshed.");

    [HttpPost("password-reset/send")]
    [AllowAnonymous]
    public async Task<IActionResult> SendPasswordReset(SendPasswordResetCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.IsFailure
            ? BadRequest(ApiResponse<object?>.Ok(result.Error!))
            : OkMessage("If the email exists, a reset link has been sent.");
    }

    [HttpPost("password-reset/reset")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.IsFailure ? BadRequest(ApiResponse<object?>.Ok(result.Error!)) : OkMessage("Password reset successful.");
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(VerifyEmailCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.IsFailure ? BadRequest(ApiResponse<object?>.Ok(result.Error!)) : OkMessage("Email verified.");
    }

    [HttpPost("devices")]
    [Authorize(Policy = Policies.CustomerOnly)]
    public async Task<IActionResult> RegisterDevice(RegisterDeviceCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.IsFailure ? BadRequest(ApiResponse<object?>.Ok(result.Error!)) : OkMessage("Device registered.");
    }

    [HttpGet("me")]
    [Authorize(Policy = Policies.CustomerOnly)]
    public async Task<IActionResult> Me(CancellationToken ct) =>
        OkResult(await _sender.Send(new GetCurrentCustomerQuery(), ct), "Current customer retrieved.");

    [HttpGet("me/sessions")]
    [Authorize(Policy = Policies.CustomerOnly)]
    public async Task<IActionResult> MySessions(CancellationToken ct) =>
        OkResult(await _sender.Send(new GetMySessionsQuery(), ct), "Sessions retrieved.");

    [HttpGet("me/devices")]
    [Authorize(Policy = Policies.CustomerOnly)]
    public async Task<IActionResult> MyDevices(CancellationToken ct) =>
        OkResult(await _sender.Send(new GetMyDevicesQuery(), ct), "Devices retrieved.");
}

using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingAnalytics.Modules.Identity.Application.Dtos;
using TradingAnalytics.Modules.Identity.Application.Mappings;
using TradingAnalytics.Modules.Identity.Domain.Entities;
using TradingAnalytics.Modules.Identity.Domain.Enums;
using TradingAnalytics.Shared.Infrastructure.Auth;
using TradingAnalytics.Shared.Infrastructure.Cache;
using TradingAnalytics.Shared.Infrastructure.Persistence;
using TradingAnalytics.Shared.Infrastructure.Session;
using TradingAnalytics.Shared.Kernel;
using TradingAnalytics.Shared.Kernel.Interfaces;
using TradingAnalytics.Shared.Kernel.Results;

namespace TradingAnalytics.Modules.Identity.Application.Commands;

/// <summary>
/// Registers a customer with email and password credentials.
/// </summary>
public sealed record RegisterWithEmailCommand(string Name, string Email, string Password) : IRequest<Result<AuthResponseDto>>;

/// <summary>
/// Starts customer registration using a phone number.
/// </summary>
public sealed record RegisterWithPhoneCommand(string Name, string Phone) : IRequest<Result<VerificationResponseDto>>;

/// <summary>
/// Completes phone registration after OTP verification.
/// </summary>
public sealed record VerifyPhoneRegistrationCommand(Guid CustomerId, Guid VerificationId, string RawOtp, string Password) : IRequest<Result<AuthResponseDto>>;

/// <summary>
/// Authenticates a customer using email and password.
/// </summary>
public sealed record LoginWithEmailCommand(string Email, string Password, SessionType SessionType, Guid? DeviceId, string? IpAddress, string? UserAgent) : IRequest<Result<AuthResponseDto>>;

/// <summary>
/// Starts phone-based login for a customer.
/// </summary>
public sealed record LoginWithPhoneCommand(string Phone) : IRequest<Result<VerificationResponseDto>>;

/// <summary>
/// Completes phone-based login after OTP verification.
/// </summary>
public sealed record VerifyOtpLoginCommand(Guid VerificationId, string RawOtp, SessionType SessionType, Guid? DeviceId, string? IpAddress, string? UserAgent) : IRequest<Result<AuthResponseDto>>;

/// <summary>
/// Authenticates an administrator.
/// </summary>
public sealed record AdminLoginCommand(string Email, string Password, string? IpAddress, string? UserAgent) : IRequest<Result<AdminAuthResponseDto>>;

/// <summary>
/// Logs out the current session.
/// </summary>
public sealed record LogoutCommand() : IRequest<Result>;

/// <summary>
/// Logs out all sessions for the current actor.
/// </summary>
public sealed record LogoutAllCommand() : IRequest<Result>;

/// <summary>
/// Refreshes an access token pair using a rotating refresh token.
/// </summary>
public sealed record RefreshTokenCommand(Guid SessionId, string RawRefreshToken) : IRequest<Result<AuthTokensDto>>;

/// <summary>
/// Starts a password-reset flow for an email address.
/// </summary>
public sealed record SendPasswordResetCommand(string Email) : IRequest<Result>;

/// <summary>
/// Resets a password using a verification token.
/// </summary>
public sealed record ResetPasswordCommand(string RawToken, string NewPassword) : IRequest<Result>;

/// <summary>
/// Verifies an email address using a verification token.
/// </summary>
public sealed record VerifyEmailCommand(string RawToken) : IRequest<Result>;

/// <summary>
/// Registers or updates a customer device.
/// </summary>
public sealed record RegisterDeviceCommand(string DeviceId, DeviceType DeviceType, string? DeviceName, string? FcmToken) : IRequest<Result>;

internal sealed class RegisterWithEmailCommandValidator : AbstractValidator<RegisterWithEmailCommand>
{
    public RegisterWithEmailCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

internal sealed class RegisterWithPhoneCommandValidator : AbstractValidator<RegisterWithPhoneCommand>
{
    public RegisterWithPhoneCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Phone).NotEmpty();
    }
}

internal sealed class LoginWithEmailCommandValidator : AbstractValidator<LoginWithEmailCommand>
{
    public LoginWithEmailCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

internal sealed class LoginWithPhoneCommandValidator : AbstractValidator<LoginWithPhoneCommand>
{
    public LoginWithPhoneCommandValidator() => RuleFor(x => x.Phone).NotEmpty();
}

internal sealed class AdminLoginCommandValidator : AbstractValidator<AdminLoginCommand>
{
    public AdminLoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

internal sealed class RegisterWithEmailHandler(
    AppDbContext db,
    IPasswordService passwordService,
    INotificationService notificationService) : IRequestHandler<RegisterWithEmailCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(RegisterWithEmailCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Customers.AnyAsync(x => x.Email == email, ct))
        {
            return Result<AuthResponseDto>.Failure("Email already exists.");
        }

        var customerResult = Customer.Create(request.Name, email, null);
        if (customerResult.IsFailure)
        {
            return Result<AuthResponseDto>.Failure(customerResult.Error!);
        }

        var customer = customerResult.Value!;
        db.Customers.Add(customer);
        db.Accounts.Add(Account.CreateCredentialForCustomer(customer.Id, email, passwordService.Hash(request.Password)));
        await db.SaveChangesAsync(ct);

        var verification = Verification.Create(customer.Id.ToString(), email, VerificationPurpose.EmailVerification, VerificationChannel.Email);
        db.Verifications.Add(verification.Verification);
        await db.SaveChangesAsync(ct);

        await notificationService.SendAsync(new SendNotificationRequest
        {
            RecipientId = customer.Id.ToString(),
            Title = "Verify your email",
            Body = $"Verification token: {verification.RawToken}",
            Type = NotificationType.Transactional,
            Surfaces = [NotificationSurface.Email]
        }, ct);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            Tokens = new AuthTokensDto(),
            Profile = customer.ToProfileDto()
        });
    }
}

internal sealed class RegisterWithPhoneHandler(
    AppDbContext db,
    INotificationService notificationService) : IRequestHandler<RegisterWithPhoneCommand, Result<VerificationResponseDto>>
{
    public async Task<Result<VerificationResponseDto>> Handle(RegisterWithPhoneCommand request, CancellationToken ct)
    {
        if (await db.Customers.AnyAsync(x => x.Phone == request.Phone.Trim(), ct))
        {
            return Result<VerificationResponseDto>.Failure("Phone already exists.");
        }

        var customerResult = Customer.Create(request.Name, null, request.Phone);
        if (customerResult.IsFailure)
        {
            return Result<VerificationResponseDto>.Failure(customerResult.Error!);
        }

        var customer = customerResult.Value!;
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);

        var verification = Verification.Create(customer.Id.ToString(), customer.Phone!, VerificationPurpose.PhoneRegistration, VerificationChannel.Sms);
        db.Verifications.Add(verification.Verification);
        await db.SaveChangesAsync(ct);

        await notificationService.SendAsync(new SendNotificationRequest
        {
            RecipientId = customer.Id.ToString(),
            Title = "Phone verification",
            Body = $"OTP: {verification.RawOtp}",
            Type = NotificationType.Transactional,
            Surfaces = [NotificationSurface.Sms]
        }, ct);

        return Result<VerificationResponseDto>.Success(new VerificationResponseDto
        {
            VerificationId = verification.Verification.Id,
            CustomerId = customer.Id
        });
    }
}

internal sealed class VerifyPhoneRegistrationHandler(
    AppDbContext db,
    IPasswordService passwordService) : IRequestHandler<VerifyPhoneRegistrationCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(VerifyPhoneRegistrationCommand request, CancellationToken ct)
    {
        var verification = await db.Verifications.FirstOrDefaultAsync(x => x.Id == request.VerificationId, ct);
        if (verification is null)
        {
            return Result<AuthResponseDto>.Failure("Verification not found.");
        }

        var consumeResult = verification.TryConsumeWithOtp(request.RawOtp);
        if (consumeResult.IsFailure)
        {
            return Result<AuthResponseDto>.Failure(consumeResult.Error!);
        }

        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == request.CustomerId, ct);
        if (customer is null || customer.Phone is null)
        {
            return Result<AuthResponseDto>.Failure("Customer not found.");
        }

        customer.MarkPhoneVerified();
        db.Accounts.Add(Account.CreateCredentialForCustomer(customer.Id, customer.Phone, passwordService.Hash(request.Password)));
        await db.SaveChangesAsync(ct);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            Tokens = new AuthTokensDto(),
            Profile = customer.ToProfileDto()
        });
    }
}

internal sealed class LoginWithEmailHandler(
    AppDbContext db,
    IPasswordService passwordService,
    IJwtService jwtService,
    ISessionStore sessionStore,
    ICacheService cacheService) : IRequestHandler<LoginWithEmailCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(LoginWithEmailCommand request, CancellationToken ct)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(
            x => x.ProviderId == Constants.Providers.Credential
                && x.AccountId == request.Email.Trim().ToLowerInvariant()
                && x.ActorType == AccountActorType.Customer,
            ct);

        if (account is null || !account.VerifyPassword(passwordService, request.Password))
        {
            return Result<AuthResponseDto>.Failure("Invalid credentials.");
        }

        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == account.CustomerId, ct);
        if (customer is null)
        {
            return Result<AuthResponseDto>.Failure("Customer not found.");
        }

        if (customer.Banned)
        {
            return Result<AuthResponseDto>.Failure("Account is banned.");
        }

        if (request.SessionType == SessionType.Mobile && request.DeviceId.HasValue)
        {
            var oldSessions = await db.CustomerSessions
                .Where(x => x.CustomerId == customer.Id && x.Type == SessionType.Mobile && x.UserDeviceId == request.DeviceId.Value)
                .ToListAsync(ct);

            foreach (var oldSession in oldSessions)
            {
                await sessionStore.RemoveAsync(oldSession.Id, ct);
            }

            db.CustomerSessions.RemoveRange(oldSessions);
        }

        var created = CustomerSession.Create(customer.Id, request.SessionType, request.DeviceId, request.IpAddress, request.UserAgent);
        db.CustomerSessions.Add(created.Session);
        await db.SaveChangesAsync(ct);

        var permissions = await IdentityAuthSupport.LoadPermissionsAsync(cacheService, db, customer.RoleId, ct);
        var roleName = await IdentityAuthSupport.LoadRoleNameAsync(db, customer.RoleId, ct);
        await sessionStore.SetAsync(created.Session.Id, created.Session.ToSessionData(roleName, permissions), created.Session.ExpiresAt - DateTime.UtcNow, ct);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            Tokens = new AuthTokensDto
            {
                AccessToken = jwtService.GenerateCustomerToken(customer.Id, customer.Email, customer.Phone, roleName, permissions, created.Session.Id),
                RefreshToken = created.RawRefreshToken,
                SessionId = created.Session.Id,
                AccessTokenExpiresInSeconds = 3600,
                RefreshTokenExpiresInSeconds = request.SessionType == SessionType.Mobile ? 2592000 : 604800
            },
            Profile = customer.ToProfileDto()
        });
    }
}

internal sealed class LoginWithPhoneHandler(
    AppDbContext db,
    INotificationService notificationService) : IRequestHandler<LoginWithPhoneCommand, Result<VerificationResponseDto>>
{
    public async Task<Result<VerificationResponseDto>> Handle(LoginWithPhoneCommand request, CancellationToken ct)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Phone == request.Phone.Trim(), ct);
        if (customer is null || customer.Phone is null)
        {
            return Result<VerificationResponseDto>.Failure("Customer not found.");
        }

        var verification = Verification.Create(customer.Id.ToString(), customer.Phone, VerificationPurpose.PhoneLogin, VerificationChannel.Sms);
        db.Verifications.Add(verification.Verification);
        await db.SaveChangesAsync(ct);

        await notificationService.SendAsync(new SendNotificationRequest
        {
            RecipientId = customer.Id.ToString(),
            Title = "Login verification",
            Body = $"OTP: {verification.RawOtp}",
            Surfaces = [NotificationSurface.Sms],
            Type = NotificationType.Transactional
        }, ct);

        return Result<VerificationResponseDto>.Success(new VerificationResponseDto
        {
            VerificationId = verification.Verification.Id,
            CustomerId = customer.Id
        });
    }
}

internal sealed class VerifyOtpLoginHandler(
    AppDbContext db,
    IJwtService jwtService,
    ISessionStore sessionStore,
    ICacheService cacheService) : IRequestHandler<VerifyOtpLoginCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(VerifyOtpLoginCommand request, CancellationToken ct)
    {
        var verification = await db.Verifications.FirstOrDefaultAsync(x => x.Id == request.VerificationId, ct);
        if (verification is null)
        {
            return Result<AuthResponseDto>.Failure("Verification not found.");
        }

        var consumeResult = verification.TryConsumeWithOtp(request.RawOtp);
        if (consumeResult.IsFailure || !Guid.TryParse(verification.Identifier, out var customerId))
        {
            return Result<AuthResponseDto>.Failure(consumeResult.Error ?? "Verification is invalid.");
        }

        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == customerId, ct);
        if (customer is null)
        {
            return Result<AuthResponseDto>.Failure("Customer not found.");
        }

        var created = CustomerSession.Create(customer.Id, request.SessionType, request.DeviceId, request.IpAddress, request.UserAgent);
        db.CustomerSessions.Add(created.Session);
        await db.SaveChangesAsync(ct);

        var permissions = await IdentityAuthSupport.LoadPermissionsAsync(cacheService, db, customer.RoleId, ct);
        var roleName = await IdentityAuthSupport.LoadRoleNameAsync(db, customer.RoleId, ct);
        await sessionStore.SetAsync(created.Session.Id, created.Session.ToSessionData(roleName, permissions), created.Session.ExpiresAt - DateTime.UtcNow, ct);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            Tokens = new AuthTokensDto
            {
                AccessToken = jwtService.GenerateCustomerToken(customer.Id, customer.Email, customer.Phone, roleName, permissions, created.Session.Id),
                RefreshToken = created.RawRefreshToken,
                SessionId = created.Session.Id,
                AccessTokenExpiresInSeconds = 3600,
                RefreshTokenExpiresInSeconds = request.SessionType == SessionType.Mobile ? 2592000 : 604800
            },
            Profile = customer.ToProfileDto()
        });
    }
}

internal sealed class AdminLoginHandler(
    AppDbContext db,
    IPasswordService passwordService,
    IJwtService jwtService,
    ISessionStore sessionStore,
    ICacheService cacheService) : IRequestHandler<AdminLoginCommand, Result<AdminAuthResponseDto>>
{
    public async Task<Result<AdminAuthResponseDto>> Handle(AdminLoginCommand request, CancellationToken ct)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(
            x => x.ProviderId == Constants.Providers.Credential
                && x.AccountId == request.Email.Trim().ToLowerInvariant()
                && x.ActorType == AccountActorType.Admin,
            ct);

        if (account is null || !account.VerifyPassword(passwordService, request.Password))
        {
            return Result<AdminAuthResponseDto>.Failure("Invalid credentials.");
        }

        var admin = await db.AdminUsers.FirstOrDefaultAsync(x => x.Id == account.AdminId, ct);
        if (admin is null)
        {
            return Result<AdminAuthResponseDto>.Failure("Admin not found.");
        }

        if (admin.Banned)
        {
            return Result<AdminAuthResponseDto>.Failure("Account is banned.");
        }

        var created = AdminSession.Create(admin.Id, SessionType.Web, request.IpAddress, request.UserAgent);
        db.AdminSessions.Add(created.Session);
        await db.SaveChangesAsync(ct);

        var permissions = await IdentityAuthSupport.LoadPermissionsAsync(cacheService, db, admin.RoleId, ct);
        var roleName = await IdentityAuthSupport.LoadRoleNameAsync(db, admin.RoleId, ct, Constants.Roles.Admin);
        await sessionStore.SetAsync(created.Session.Id, created.Session.ToSessionData(roleName, permissions), created.Session.ExpiresAt - DateTime.UtcNow, ct);

        return Result<AdminAuthResponseDto>.Success(new AdminAuthResponseDto
        {
            Tokens = new AuthTokensDto
            {
                AccessToken = jwtService.GenerateAdminToken(admin.Id, admin.Email!, roleName, permissions, created.Session.Id),
                RefreshToken = created.RawRefreshToken,
                SessionId = created.Session.Id,
                AccessTokenExpiresInSeconds = 3600,
                RefreshTokenExpiresInSeconds = 28800
            },
            AdminId = admin.Id,
            Email = admin.Email!,
            RoleId = admin.RoleId
        });
    }
}

internal static class IdentityAuthSupport
{
    public static Task<List<string>> LoadPermissionsAsync(ICacheService cacheService, AppDbContext db, int? roleId, CancellationToken ct)
    {
        if (!roleId.HasValue)
        {
            return Task.FromResult(new List<string>());
        }

        return cacheService.GetOrSetAsync(
            $"role_permissions:{roleId.Value}",
            async () => await db.RolePermissions
                .Where(x => x.RoleId == roleId.Value)
                .Join(db.Permissions, rp => rp.PermissionId, p => p.Id, (_, p) => p.Code)
                .ToListAsync(ct),
            TimeSpan.FromMinutes(10),
            ct);
    }

    public static async Task<string> LoadRoleNameAsync(AppDbContext db, int? roleId, CancellationToken ct, string fallback = "customer")
    {
        if (!roleId.HasValue)
        {
            return fallback;
        }

        return await db.Roles.Where(x => x.Id == roleId.Value).Select(x => x.Name).FirstOrDefaultAsync(ct) ?? fallback;
    }

    public static AuthTokensDto ToTokens(string accessToken, string refreshToken, Guid sessionId, int refreshTokenExpiresInSeconds) =>
        new()
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            SessionId = sessionId,
            AccessTokenExpiresInSeconds = 3600,
            RefreshTokenExpiresInSeconds = refreshTokenExpiresInSeconds
        };
}

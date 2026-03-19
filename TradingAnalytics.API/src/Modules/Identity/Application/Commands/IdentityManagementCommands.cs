using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingAnalytics.Modules.Identity.Application.Dtos;
using TradingAnalytics.Modules.Identity.Application.Mappings;
using TradingAnalytics.Modules.Identity.Domain.Entities;
using TradingAnalytics.Modules.Identity.Domain.Enums;
using TradingAnalytics.Shared.Infrastructure.Auth;
using TradingAnalytics.Shared.Infrastructure.Persistence;
using TradingAnalytics.Shared.Infrastructure.Session;
using TradingAnalytics.Shared.Kernel.Interfaces;
using TradingAnalytics.Shared.Kernel.Results;

namespace TradingAnalytics.Modules.Identity.Application.Commands;

/// <summary>
/// Creates a customer as an administrator.
/// </summary>
/// <param name="Name">The customer name.</param>
/// <param name="Email">The customer email.</param>
/// <param name="Password">The plaintext password.</param>
public sealed record AdminCreateCustomerCommand(string Name, string Email, string Password) : IRequest<Result<AuthResponseDto>>;

/// <summary>
/// Bans a customer as an administrator.
/// </summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="Reason">The ban reason.</param>
public sealed record AdminBanCustomerCommand(Guid CustomerId, string Reason) : IRequest<Result>;

internal sealed class LogoutHandler(AppDbContext db, ISessionStore sessionStore) : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken ct)
    {
        var customerSession = await db.CustomerSessions.FirstOrDefaultAsync(x => x.Token == request.RawToken, ct);
        if (customerSession is not null)
        {
            db.CustomerSessions.Remove(customerSession);
        }

        var adminSession = await db.AdminSessions.FirstOrDefaultAsync(x => x.Token == request.RawToken, ct);
        if (adminSession is not null)
        {
            db.AdminSessions.Remove(adminSession);
        }

        await db.SaveChangesAsync(ct);
        await sessionStore.RemoveAsync(request.RawToken, ct);
        return Result.Success();
    }
}

internal sealed class LogoutAllHandler(
    AppDbContext db,
    ISessionStore sessionStore,
    ICurrentUserService currentUser) : IRequestHandler<LogoutAllCommand, Result>
{
    public async Task<Result> Handle(LogoutAllCommand request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
        {
            return Result.Failure("User is not authenticated.");
        }

        if (currentUser.IsAdmin)
        {
            db.AdminSessions.RemoveRange(await db.AdminSessions.Where(x => x.AdminId == currentUser.UserId.Value).ToListAsync(ct));
        }
        else
        {
            db.CustomerSessions.RemoveRange(await db.CustomerSessions.Where(x => x.CustomerId == currentUser.UserId.Value).ToListAsync(ct));
        }

        await db.SaveChangesAsync(ct);
        await sessionStore.RemoveAllForUserAsync(currentUser.UserId.Value, ct);
        return Result.Success();
    }
}

internal sealed class RefreshTokenHandler(
    AppDbContext db,
    ISessionStore sessionStore,
    IJwtService jwtService) : IRequestHandler<RefreshTokenCommand, Result<string>>
{
    public async Task<Result<string>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var cached = await sessionStore.GetAsync(request.RawToken, ct);
        if (cached is not null)
        {
            if (cached.ActorType == "admin")
            {
                var admin = await db.AdminUsers.FirstOrDefaultAsync(x => x.Id == cached.UserId, ct);
                return admin is null
                    ? Result<string>.Failure("Session expired.")
                    : Result<string>.Success(jwtService.GenerateAdminToken(admin.Id, admin.Email!, admin.RoleId?.ToString() ?? "admin"));
            }

            var customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == cached.UserId, ct);
            return customer is null
                ? Result<string>.Failure("Session expired.")
                : Result<string>.Success(jwtService.GenerateCustomerToken(customer.Id, customer.Email, customer.Phone, "customer"));
        }

        var customerSession = await db.CustomerSessions.FirstOrDefaultAsync(x => x.Token == request.RawToken, ct);
        if (customerSession is not null && !customerSession.IsExpired)
        {
            var customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == customerSession.CustomerId, ct);
            return customer is null
                ? Result<string>.Failure("Session expired.")
                : Result<string>.Success(jwtService.GenerateCustomerToken(customer.Id, customer.Email, customer.Phone, "customer"));
        }

        var adminSession = await db.AdminSessions.FirstOrDefaultAsync(x => x.Token == request.RawToken, ct);
        if (adminSession is not null && !adminSession.IsExpired)
        {
            var admin = await db.AdminUsers.FirstOrDefaultAsync(x => x.Id == adminSession.AdminId, ct);
            return admin is null
                ? Result<string>.Failure("Session expired.")
                : Result<string>.Success(jwtService.GenerateAdminToken(admin.Id, admin.Email!, admin.RoleId?.ToString() ?? "admin"));
        }

        return Result<string>.Failure("Session expired.");
    }
}

internal sealed class SendPasswordResetHandler(
    AppDbContext db,
    INotificationService notificationService) : IRequestHandler<SendPasswordResetCommand, Result>
{
    public async Task<Result> Handle(SendPasswordResetCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var account = await db.Accounts.FirstOrDefaultAsync(
            x => x.ProviderId == "credential" && x.AccountId == email && x.ActorType == AccountActorType.Customer,
            ct);

        if (account?.CustomerId is Guid customerId)
        {
            var verification = Verification.Create(customerId.ToString(), email, VerificationPurpose.PasswordReset, VerificationChannel.Email);
            db.Verifications.Add(verification.Verification);
            await db.SaveChangesAsync(ct);

            await notificationService.SendAsync(new SendNotificationRequest
            {
                RecipientId = customerId.ToString(),
                Title = "Password reset",
                Body = $"Reset token: {verification.RawToken}",
                Surfaces = [NotificationSurface.Email],
                Type = NotificationType.Transactional
            }, ct);
        }

        return Result.Success();
    }
}

internal sealed class ResetPasswordHandler(
    AppDbContext db,
    IPasswordService passwordService,
    ISessionStore sessionStore) : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.RawToken))).ToLowerInvariant();
        var verification = await db.Verifications.FirstOrDefaultAsync(
            x => x.TokenHash == tokenHash && x.Purpose == VerificationPurpose.PasswordReset,
            ct);
        if (verification is null)
        {
            return Result.Failure("Verification not found.");
        }

        var consumeResult = verification.TryConsumeWithToken(request.RawToken);
        if (consumeResult.IsFailure)
        {
            return Result.Failure(consumeResult.Error!);
        }

        var account = await db.Accounts.FirstOrDefaultAsync(
            x => x.CustomerId != null && x.CustomerId.Value.ToString() == verification.Identifier && x.ProviderId == "credential",
            ct);
        if (account is null)
        {
            return Result.Failure("Account not found.");
        }

        account.UpdatePassword(passwordService.Hash(request.NewPassword));

        if (account.CustomerId.HasValue)
        {
            db.CustomerSessions.RemoveRange(await db.CustomerSessions.Where(x => x.CustomerId == account.CustomerId.Value).ToListAsync(ct));
            await sessionStore.RemoveAllForUserAsync(account.CustomerId.Value, ct);
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

internal sealed class VerifyEmailHandler(AppDbContext db) : IRequestHandler<VerifyEmailCommand, Result>
{
    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken ct)
    {
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.RawToken))).ToLowerInvariant();
        var verification = await db.Verifications.FirstOrDefaultAsync(
            x => x.TokenHash == tokenHash && x.Purpose == VerificationPurpose.EmailVerification,
            ct);
        if (verification is null)
        {
            return Result.Failure("Verification not found.");
        }

        var consumeResult = verification.TryConsumeWithToken(request.RawToken);
        if (consumeResult.IsFailure || !Guid.TryParse(verification.Identifier, out var customerId))
        {
            return Result.Failure(consumeResult.Error ?? "Verification is invalid.");
        }

        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == customerId, ct);
        if (customer is null)
        {
            return Result.Failure("Customer not found.");
        }

        customer.MarkEmailVerified();
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

internal sealed class RegisterDeviceHandler(
    AppDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<RegisterDeviceCommand, Result>
{
    public async Task<Result> Handle(RegisterDeviceCommand request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
        {
            return Result.Failure("User is not authenticated.");
        }

        var device = await db.UserDevices.FirstOrDefaultAsync(
            x => x.CustomerId == currentUser.UserId.Value && x.DeviceId == request.DeviceId,
            ct);

        if (device is null)
        {
            db.UserDevices.Add(UserDevice.Register(currentUser.UserId.Value, request.DeviceId, request.DeviceType, request.DeviceName, request.FcmToken));
        }
        else
        {
            device.UpdateFcmToken(request.FcmToken);
            device.RecordActivity();
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

internal sealed class AdminCreateCustomerHandler(
    AppDbContext db,
    IPasswordService passwordService,
    IJwtService jwtService,
    IAuditLogger auditLogger) : IRequestHandler<AdminCreateCustomerCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(AdminCreateCustomerCommand request, CancellationToken ct)
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
        customer.MarkEmailVerified();
        db.Customers.Add(customer);
        db.Accounts.Add(Account.CreateCredentialForCustomer(customer.Id, email, passwordService.Hash(request.Password)));
        await db.SaveChangesAsync(ct);

        await auditLogger.LogAsync(new AuditLogEntry
        {
            Action = "admin_create_customer",
            Module = "identity",
            Status = "success",
            ResourceId = customer.Id.ToString(),
            ResourceType = "customer"
        }, ct);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = jwtService.GenerateCustomerToken(customer.Id, customer.Email, customer.Phone, "customer"),
            Customer = customer.ToProfileDto()
        });
    }
}

internal sealed class AdminBanCustomerHandler(
    AppDbContext db,
    ISessionStore sessionStore,
    IAuditLogger auditLogger) : IRequestHandler<AdminBanCustomerCommand, Result>
{
    public async Task<Result> Handle(AdminBanCustomerCommand request, CancellationToken ct)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == request.CustomerId, ct);
        if (customer is null)
        {
            return Result.Failure("Customer not found.");
        }

        var banResult = customer.Ban(request.Reason);
        if (banResult.IsFailure)
        {
            return banResult;
        }

        db.CustomerSessions.RemoveRange(await db.CustomerSessions.Where(x => x.CustomerId == customer.Id).ToListAsync(ct));
        await sessionStore.RemoveAllForUserAsync(customer.Id, ct);
        await db.SaveChangesAsync(ct);

        await auditLogger.LogAsync(new AuditLogEntry
        {
            Action = "admin_ban_customer",
            Module = "identity",
            Status = "success",
            ResourceId = customer.Id.ToString(),
            ResourceType = "customer",
            Reason = request.Reason
        }, ct);

        return Result.Success();
    }
}

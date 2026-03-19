using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingAnalytics.Modules.Identity.Application.Dtos;
using TradingAnalytics.Modules.Identity.Application.Mappings;
using TradingAnalytics.Modules.Identity.Domain.Entities;
using TradingAnalytics.Shared.Infrastructure.Auth;
using TradingAnalytics.Shared.Infrastructure.Cache;
using TradingAnalytics.Shared.Infrastructure.Persistence;
using TradingAnalytics.Shared.Infrastructure.Session;
using TradingAnalytics.Shared.Kernel;
using TradingAnalytics.Shared.Kernel.Interfaces;
using TradingAnalytics.Shared.Kernel.Results;

namespace TradingAnalytics.Modules.Identity.Application.Commands;

/// <summary>
/// Creates a customer as an administrator.
/// </summary>
public sealed record AdminCreateCustomerCommand(string Name, string Email, string Password) : IRequest<Result<AuthResponseDto>>;

/// <summary>
/// Bans a customer as an administrator.
/// </summary>
public sealed record AdminBanCustomerCommand(Guid CustomerId, string Reason) : IRequest<Result>;

/// <summary>
/// Revokes a single customer-owned session.
/// </summary>
public sealed record RevokeSessionCommand(Guid SessionId) : IRequest<Result>;

/// <summary>
/// Creates a new role.
/// </summary>
public sealed record CreateRoleCommand(string Name, string? Description, List<int> PermissionIds) : IRequest<Result<RoleDto>>;

/// <summary>
/// Updates the permissions assigned to a role.
/// </summary>
public sealed record UpdateRolePermissionsCommand(int RoleId, List<int> PermissionIds) : IRequest<Result>;

/// <summary>
/// Assigns a role to a customer.
/// </summary>
public sealed record AssignRoleToCustomerCommand(Guid CustomerId, int RoleId) : IRequest<Result>;

/// <summary>
/// Assigns a role to an administrator.
/// </summary>
public sealed record AssignRoleToAdminCommand(Guid AdminId, int RoleId) : IRequest<Result>;

/// <summary>
/// Deletes a role.
/// </summary>
public sealed record DeleteRoleCommand(int RoleId) : IRequest<Result>;

internal sealed class LogoutHandler(
    AppDbContext db,
    ISessionStore sessionStore,
    ICurrentUserService currentUser) : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue || !currentUser.SessionId.HasValue)
        {
            return Result.Failure("Session not found.");
        }

        if (currentUser.IsAdmin)
        {
            var adminSession = await db.AdminSessions.FirstOrDefaultAsync(
                x => x.Id == currentUser.SessionId.Value && x.AdminId == currentUser.UserId.Value,
                ct);

            if (adminSession is null)
            {
                return Result.Failure("Session not found.");
            }

            db.AdminSessions.Remove(adminSession);
        }
        else
        {
            var customerSession = await db.CustomerSessions.FirstOrDefaultAsync(
                x => x.Id == currentUser.SessionId.Value && x.CustomerId == currentUser.UserId.Value,
                ct);

            if (customerSession is null)
            {
                return Result.Failure("Session not found.");
            }

            db.CustomerSessions.Remove(customerSession);
        }

        await db.SaveChangesAsync(ct);
        await sessionStore.RemoveAsync(currentUser.SessionId.Value, ct);
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
    IJwtService jwtService,
    ICacheService cacheService) : IRequestHandler<RefreshTokenCommand, Result<AuthTokensDto>>
{
    public async Task<Result<AuthTokensDto>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var incomingHash = RefreshTokenHasher.Hash(request.RawRefreshToken);
        var customerSession = await db.CustomerSessions.FirstOrDefaultAsync(x => x.Id == request.SessionId && !x.IsExpired, ct);
        if (customerSession is not null)
        {
            return await RefreshCustomerSessionAsync(customerSession, incomingHash, ct);
        }

        var adminSession = await db.AdminSessions.FirstOrDefaultAsync(x => x.Id == request.SessionId && !x.IsExpired, ct);
        if (adminSession is not null)
        {
            return await RefreshAdminSessionAsync(adminSession, incomingHash, ct);
        }

        return Result<AuthTokensDto>.Failure("Session not found or expired. Please log in again.");

        async Task<Result<AuthTokensDto>> RefreshCustomerSessionAsync(CustomerSession session, string hash, CancellationToken cancellationToken)
        {
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hash), Encoding.UTF8.GetBytes(session.RefreshTokenHash)))
            {
                db.CustomerSessions.Remove(session);
                await sessionStore.RemoveAsync(session.Id, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                return Result<AuthTokensDto>.Failure("Invalid refresh token. Session has been terminated for security.");
            }

            var customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == session.CustomerId, cancellationToken);
            if (customer is null || customer.Banned)
            {
                return Result<AuthTokensDto>.Failure("Account unavailable.");
            }

            var (newSession, rawRefreshToken) = CustomerSession.Create(customer.Id, session.Type, session.UserDeviceId, session.IpAddress, session.UserAgent);
            db.CustomerSessions.Remove(session);
            await db.CustomerSessions.AddAsync(newSession, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await sessionStore.RemoveAsync(session.Id, cancellationToken);

            var permissions = await IdentityAuthSupport.LoadPermissionsAsync(cacheService, db, customer.RoleId, cancellationToken);
            var roleName = await IdentityAuthSupport.LoadRoleNameAsync(db, customer.RoleId, cancellationToken);
            await sessionStore.SetAsync(newSession.Id, newSession.ToSessionData(roleName, permissions), newSession.ExpiresAt - DateTime.UtcNow, cancellationToken);

            return Result<AuthTokensDto>.Success(IdentityAuthSupport.ToTokens(
                jwtService.GenerateCustomerToken(customer.Id, customer.Email, customer.Phone, roleName, permissions, newSession.Id),
                rawRefreshToken,
                newSession.Id,
                newSession.Type == Domain.Enums.SessionType.Mobile ? 2592000 : 604800));
        }

        async Task<Result<AuthTokensDto>> RefreshAdminSessionAsync(AdminSession session, string hash, CancellationToken cancellationToken)
        {
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hash), Encoding.UTF8.GetBytes(session.RefreshTokenHash)))
            {
                db.AdminSessions.Remove(session);
                await sessionStore.RemoveAsync(session.Id, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                return Result<AuthTokensDto>.Failure("Invalid refresh token. Session has been terminated for security.");
            }

            var admin = await db.AdminUsers.FirstOrDefaultAsync(x => x.Id == session.AdminId, cancellationToken);
            if (admin is null || admin.Banned || string.IsNullOrWhiteSpace(admin.Email))
            {
                return Result<AuthTokensDto>.Failure("Account unavailable.");
            }

            var (newSession, rawRefreshToken) = AdminSession.Create(admin.Id, session.Type, session.IpAddress, session.UserAgent);
            db.AdminSessions.Remove(session);
            await db.AdminSessions.AddAsync(newSession, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await sessionStore.RemoveAsync(session.Id, cancellationToken);

            var permissions = await IdentityAuthSupport.LoadPermissionsAsync(cacheService, db, admin.RoleId, cancellationToken);
            var roleName = await IdentityAuthSupport.LoadRoleNameAsync(db, admin.RoleId, cancellationToken, Constants.Roles.Admin);
            await sessionStore.SetAsync(newSession.Id, newSession.ToSessionData(roleName, permissions), newSession.ExpiresAt - DateTime.UtcNow, cancellationToken);

            return Result<AuthTokensDto>.Success(IdentityAuthSupport.ToTokens(
                jwtService.GenerateAdminToken(admin.Id, admin.Email, roleName, permissions, newSession.Id),
                rawRefreshToken,
                newSession.Id,
                28800));
        }
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
            x => x.ProviderId == Constants.Providers.Credential && x.AccountId == email && x.ActorType == Domain.Enums.AccountActorType.Customer,
            ct);

        if (account?.CustomerId is Guid customerId)
        {
            var verification = Verification.Create(customerId.ToString(), email, Domain.Enums.VerificationPurpose.PasswordReset, Domain.Enums.VerificationChannel.Email);
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
            x => x.TokenHash == tokenHash && x.Purpose == Domain.Enums.VerificationPurpose.PasswordReset,
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
            x => x.CustomerId != null && x.CustomerId.Value.ToString() == verification.Identifier && x.ProviderId == Constants.Providers.Credential,
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
            x => x.TokenHash == tokenHash && x.Purpose == Domain.Enums.VerificationPurpose.EmailVerification,
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

internal sealed class RevokeSessionHandler(
    AppDbContext db,
    ISessionStore sessionStore,
    ICurrentUserService currentUser) : IRequestHandler<RevokeSessionCommand, Result>
{
    public async Task<Result> Handle(RevokeSessionCommand request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
        {
            return Result.Failure("User is not authenticated.");
        }

        var session = await db.CustomerSessions.FirstOrDefaultAsync(
            x => x.Id == request.SessionId && x.CustomerId == currentUser.UserId.Value,
            ct);

        if (session is null)
        {
            return Result.Failure("Session not found.");
        }

        db.CustomerSessions.Remove(session);
        await db.SaveChangesAsync(ct);
        await sessionStore.RemoveAsync(request.SessionId, ct);
        return Result.Success();
    }
}

internal sealed class AdminCreateCustomerHandler(
    AppDbContext db,
    IPasswordService passwordService,
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
            Tokens = new AuthTokensDto(),
            Profile = customer.ToProfileDto()
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

internal sealed class CreateRoleHandler(
    AppDbContext db,
    IAuditLogger auditLogger) : IRequestHandler<CreateRoleCommand, Result<RoleDto>>
{
    public async Task<Result<RoleDto>> Handle(CreateRoleCommand request, CancellationToken ct)
    {
        var normalizedName = request.Name.Trim();
        if (await db.Roles.AnyAsync(x => x.Name == normalizedName, ct))
        {
            return Result<RoleDto>.Failure("Role already exists.");
        }

        var role = new Role
        {
            Name = normalizedName,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);

        if (request.PermissionIds.Count > 0)
        {
            var mappings = request.PermissionIds.Distinct().Select(permissionId => new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permissionId
            });

            db.RolePermissions.AddRange(mappings);
            await db.SaveChangesAsync(ct);
        }

        await auditLogger.LogAsync(new AuditLogEntry
        {
            Action = "create_role",
            Module = "identity",
            Status = "success",
            ResourceId = role.Id.ToString(),
            ResourceType = "role"
        }, ct);

        return Result<RoleDto>.Success(new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole
        });
    }
}

internal sealed class UpdateRolePermissionsHandler(
    AppDbContext db,
    ICacheService cacheService) : IRequestHandler<UpdateRolePermissionsCommand, Result>
{
    public async Task<Result> Handle(UpdateRolePermissionsCommand request, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(x => x.Id == request.RoleId, ct);
        if (role is null)
        {
            return Result.Failure("Role not found.");
        }

        if (role.IsSystemRole)
        {
            return Result.Failure("System roles cannot be modified.");
        }

        db.RolePermissions.RemoveRange(await db.RolePermissions.Where(x => x.RoleId == request.RoleId).ToListAsync(ct));
        db.RolePermissions.AddRange(request.PermissionIds.Distinct().Select(permissionId => new RolePermission
        {
            RoleId = request.RoleId,
            PermissionId = permissionId
        }));

        role.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await cacheService.RemoveAsync($"role_permissions:{request.RoleId}", ct);
        return Result.Success();
    }
}

internal sealed class AssignRoleToCustomerHandler(
    AppDbContext db,
    IAuditLogger auditLogger,
    ICacheService cacheService) : IRequestHandler<AssignRoleToCustomerCommand, Result>
{
    public async Task<Result> Handle(AssignRoleToCustomerCommand request, CancellationToken ct)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == request.CustomerId, ct);
        if (customer is null)
        {
            return Result.Failure("Customer not found.");
        }

        if (!await db.Roles.AnyAsync(x => x.Id == request.RoleId, ct))
        {
            return Result.Failure("Role not found.");
        }

        customer.AssignRole(request.RoleId);
        await db.SaveChangesAsync(ct);
        await cacheService.RemoveAsync($"role_permissions:{request.RoleId}", ct);
        await auditLogger.LogAsync(new AuditLogEntry
        {
            Action = "assign_role_customer",
            Module = "identity",
            Status = "success",
            ResourceId = customer.Id.ToString(),
            ResourceType = "customer"
        }, ct);

        return Result.Success();
    }
}

internal sealed class AssignRoleToAdminHandler(
    AppDbContext db,
    IAuditLogger auditLogger,
    ICacheService cacheService) : IRequestHandler<AssignRoleToAdminCommand, Result>
{
    public async Task<Result> Handle(AssignRoleToAdminCommand request, CancellationToken ct)
    {
        var admin = await db.AdminUsers.FirstOrDefaultAsync(x => x.Id == request.AdminId, ct);
        if (admin is null)
        {
            return Result.Failure("Admin not found.");
        }

        if (!await db.Roles.AnyAsync(x => x.Id == request.RoleId, ct))
        {
            return Result.Failure("Role not found.");
        }

        admin.AssignRole(request.RoleId);
        await db.SaveChangesAsync(ct);
        await cacheService.RemoveAsync($"role_permissions:{request.RoleId}", ct);
        await auditLogger.LogAsync(new AuditLogEntry
        {
            Action = "assign_role_admin",
            Module = "identity",
            Status = "success",
            ResourceId = admin.Id.ToString(),
            ResourceType = "admin"
        }, ct);

        return Result.Success();
    }
}

internal sealed class DeleteRoleHandler(
    AppDbContext db,
    ICacheService cacheService) : IRequestHandler<DeleteRoleCommand, Result>
{
    public async Task<Result> Handle(DeleteRoleCommand request, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(x => x.Id == request.RoleId, ct);
        if (role is null)
        {
            return Result.Failure("Role not found.");
        }

        if (role.IsSystemRole)
        {
            return Result.Failure("Cannot delete a system role.");
        }

        db.Roles.Remove(role);
        await db.SaveChangesAsync(ct);
        await cacheService.RemoveAsync($"role_permissions:{request.RoleId}", ct);
        return Result.Success();
    }
}

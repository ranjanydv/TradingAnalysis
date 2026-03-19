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
using TradingAnalytics.Shared.Infrastructure.Persistence;
using TradingAnalytics.Shared.Infrastructure.Session;
using TradingAnalytics.Shared.Kernel.Interfaces;
using TradingAnalytics.Shared.Kernel.Results;

namespace TradingAnalytics.Modules.Identity.Application.Commands;

public sealed record RegisterWithEmailCommand(string Name, string Email, string Password) : IRequest<Result<AuthResponseDto>>;
public sealed record RegisterWithPhoneCommand(string Name, string Phone) : IRequest<Result<VerificationResponseDto>>;
public sealed record VerifyPhoneRegistrationCommand(Guid CustomerId, Guid VerificationId, string RawOtp, string Password) : IRequest<Result<AuthResponseDto>>;
public sealed record LoginWithEmailCommand(string Email, string Password, SessionType SessionType, Guid? DeviceId, string? IpAddress, string? UserAgent) : IRequest<Result<AuthResponseDto>>;
public sealed record LoginWithPhoneCommand(string Phone) : IRequest<Result<VerificationResponseDto>>;
public sealed record VerifyOtpLoginCommand(Guid VerificationId, string RawOtp, SessionType SessionType, Guid? DeviceId, string? IpAddress, string? UserAgent) : IRequest<Result<AuthResponseDto>>;
public sealed record AdminLoginCommand(string Email, string Password, string? IpAddress, string? UserAgent) : IRequest<Result<AdminAuthResponseDto>>;
public sealed record LogoutCommand(string RawToken) : IRequest<Result>;
public sealed record LogoutAllCommand() : IRequest<Result>;
public sealed record RefreshTokenCommand(string RawToken) : IRequest<Result<string>>;
public sealed record SendPasswordResetCommand(string Email) : IRequest<Result>;
public sealed record ResetPasswordCommand(string RawToken, string NewPassword) : IRequest<Result>;
public sealed record VerifyEmailCommand(string RawToken) : IRequest<Result>;
public sealed record RegisterDeviceCommand(string DeviceId, DeviceType DeviceType, string? DeviceName, string? FcmToken) : IRequest<Result>;

public sealed class RegisterWithEmailCommandValidator : AbstractValidator<RegisterWithEmailCommand>
{
    public RegisterWithEmailCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public sealed class RegisterWithPhoneCommandValidator : AbstractValidator<RegisterWithPhoneCommand>
{
    public RegisterWithPhoneCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Phone).NotEmpty();
    }
}

public sealed class LoginWithEmailCommandValidator : AbstractValidator<LoginWithEmailCommand>
{
    public LoginWithEmailCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginWithPhoneCommandValidator : AbstractValidator<LoginWithPhoneCommand>
{
    public LoginWithPhoneCommandValidator() => RuleFor(x => x.Phone).NotEmpty();
}

public sealed class AdminLoginCommandValidator : AbstractValidator<AdminLoginCommand>
{
    public AdminLoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class RegisterWithEmailHandler(
    AppDbContext db,
    IPasswordService passwordService,
    IJwtService jwtService,
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
            AccessToken = jwtService.GenerateCustomerToken(customer.Id, customer.Email, customer.Phone, "customer"),
            Customer = customer.ToProfileDto()
        });
    }
}

public sealed class RegisterWithPhoneHandler(
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

public sealed class VerifyPhoneRegistrationHandler(
    AppDbContext db,
    IPasswordService passwordService,
    IJwtService jwtService) : IRequestHandler<VerifyPhoneRegistrationCommand, Result<AuthResponseDto>>
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
            AccessToken = jwtService.GenerateCustomerToken(customer.Id, customer.Email, customer.Phone, "customer"),
            Customer = customer.ToProfileDto()
        });
    }
}

public sealed class LoginWithEmailHandler(
    AppDbContext db,
    IPasswordService passwordService,
    IJwtService jwtService,
    ISessionStore sessionStore) : IRequestHandler<LoginWithEmailCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(LoginWithEmailCommand request, CancellationToken ct)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(
            x => x.ProviderId == "credential" && x.AccountId == request.Email.Trim().ToLowerInvariant() && x.ActorType == AccountActorType.Customer,
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

        if (request.SessionType == SessionType.Mobile)
        {
            await sessionStore.RemoveAllForUserAsync(customer.Id, ct);
            db.CustomerSessions.RemoveRange(await db.CustomerSessions.Where(x => x.CustomerId == customer.Id && x.Type == SessionType.Mobile).ToListAsync(ct));
        }

        var created = CustomerSession.Create(customer.Id, request.SessionType, request.DeviceId, request.IpAddress, request.UserAgent);
        db.CustomerSessions.Add(created.Session);
        await db.SaveChangesAsync(ct);
        await sessionStore.SetAsync(created.RawToken, created.Session.ToSessionData("customer"), created.Session.ExpiresAt - DateTime.UtcNow, ct);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = jwtService.GenerateCustomerToken(customer.Id, customer.Email, customer.Phone, "customer"),
            SessionToken = created.RawToken,
            Customer = customer.ToProfileDto()
        });
    }
}

public sealed class LoginWithPhoneHandler(
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

public sealed class VerifyOtpLoginHandler(
    AppDbContext db,
    IJwtService jwtService,
    ISessionStore sessionStore) : IRequestHandler<VerifyOtpLoginCommand, Result<AuthResponseDto>>
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
        await sessionStore.SetAsync(created.RawToken, created.Session.ToSessionData("customer"), created.Session.ExpiresAt - DateTime.UtcNow, ct);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = jwtService.GenerateCustomerToken(customer.Id, customer.Email, customer.Phone, "customer"),
            SessionToken = created.RawToken,
            Customer = customer.ToProfileDto()
        });
    }
}

public sealed class AdminLoginHandler(
    AppDbContext db,
    IPasswordService passwordService,
    IJwtService jwtService,
    ISessionStore sessionStore) : IRequestHandler<AdminLoginCommand, Result<AdminAuthResponseDto>>
{
    public async Task<Result<AdminAuthResponseDto>> Handle(AdminLoginCommand request, CancellationToken ct)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(
            x => x.ProviderId == "credential" && x.AccountId == request.Email.Trim().ToLowerInvariant() && x.ActorType == AccountActorType.Admin,
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
        await sessionStore.SetAsync(created.RawToken, created.Session.ToSessionData(admin.RoleId?.ToString() ?? "admin"), created.Session.ExpiresAt - DateTime.UtcNow, ct);

        return Result<AdminAuthResponseDto>.Success(new AdminAuthResponseDto
        {
            AccessToken = jwtService.GenerateAdminToken(admin.Id, admin.Email!, admin.RoleId?.ToString() ?? "admin"),
            SessionToken = created.RawToken,
            AdminId = admin.Id,
            Email = admin.Email!,
            RoleId = admin.RoleId
        });
    }
}

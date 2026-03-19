using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingAnalytics.Modules.Subscriptions.Application.Dtos;
using TradingAnalytics.Modules.Subscriptions.Application.Mappings;
using TradingAnalytics.Modules.Subscriptions.Domain.Entities;
using TradingAnalytics.Modules.Subscriptions.Domain.Enums;
using TradingAnalytics.Shared.Infrastructure.Cache;
using TradingAnalytics.Shared.Infrastructure.Persistence;
using TradingAnalytics.Shared.Kernel.Interfaces;
using TradingAnalytics.Shared.Kernel.Results;

namespace TradingAnalytics.Modules.Subscriptions.Application.Commands;

/// <summary>
/// Creates a new subscription module.
/// </summary>
public sealed record AdminCreateModuleCommand(string Slug, string Name, string? Description) : IRequest<Result<ModuleSummaryDto>>;

/// <summary>
/// Creates a new tier for a module.
/// </summary>
public sealed record AdminCreateTierCommand(Guid ModuleId, string Name, string? Description, int DisplayOrder) : IRequest<Result<SubscriptionTierDto>>;

/// <summary>
/// Replaces the configured prices for a tier.
/// </summary>
public sealed record AdminUpdateTierPricesCommand(Guid TierId, List<TierPriceInput> Prices) : IRequest<Result>;

/// <summary>
/// Grants access to a customer.
/// </summary>
public sealed record AdminGrantAccessCommand(Guid CustomerId, Guid TierId, DateTime EndsAtUtc, string? Note) : IRequest<Result<UserSubscriptionDto>>;

/// <summary>
/// Extends a customer subscription.
/// </summary>
public sealed record AdminExtendSubscriptionCommand(Guid SubscriptionId, DateTime EndsAtUtc, string? Note) : IRequest<Result>;

/// <summary>
/// Revokes a customer subscription.
/// </summary>
public sealed record AdminRevokeSubscriptionCommand(Guid SubscriptionId, string? Note) : IRequest<Result>;

/// <summary>
/// Describes a tier-price input payload.
/// </summary>
public sealed record TierPriceInput(string Currency, decimal Amount, BillingCycle BillingCycle, int? TrialDays);

/// <summary>
/// Validates module creation commands.
/// </summary>
public sealed class AdminCreateModuleCommandValidator : AbstractValidator<AdminCreateModuleCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdminCreateModuleCommandValidator"/> class.
    /// </summary>
    public AdminCreateModuleCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

/// <summary>
/// Validates tier creation commands.
/// </summary>
public sealed class AdminCreateTierCommandValidator : AbstractValidator<AdminCreateTierCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdminCreateTierCommandValidator"/> class.
    /// </summary>
    public AdminCreateTierCommandValidator()
    {
        RuleFor(x => x.ModuleId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

/// <summary>
/// Validates tier-price updates.
/// </summary>
public sealed class AdminUpdateTierPricesCommandValidator : AbstractValidator<AdminUpdateTierPricesCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdminUpdateTierPricesCommandValidator"/> class.
    /// </summary>
    public AdminUpdateTierPricesCommandValidator()
    {
        RuleFor(x => x.TierId).NotEmpty();
        RuleFor(x => x.Prices).NotEmpty();
    }
}

/// <summary>
/// Validates access grants.
/// </summary>
public sealed class AdminGrantAccessCommandValidator : AbstractValidator<AdminGrantAccessCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdminGrantAccessCommandValidator"/> class.
    /// </summary>
    public AdminGrantAccessCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.TierId).NotEmpty();
        RuleFor(x => x.EndsAtUtc).GreaterThan(DateTime.UtcNow);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

internal sealed class AdminCreateModuleHandler(
    AppDbContext db,
    IAuditLogger auditLogger) : IRequestHandler<AdminCreateModuleCommand, Result<ModuleSummaryDto>>
{
    public async Task<Result<ModuleSummaryDto>> Handle(AdminCreateModuleCommand request, CancellationToken ct)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await db.AccessModules.AnyAsync(x => x.Slug == slug, ct))
        {
            return Result<ModuleSummaryDto>.Failure("Module slug already exists.");
        }

        var moduleResult = AccessModule.Create(slug, request.Name, request.Description);
        if (moduleResult.IsFailure)
        {
            return Result<ModuleSummaryDto>.Failure(moduleResult.Error!);
        }

        db.AccessModules.Add(moduleResult.Value!);
        await db.SaveChangesAsync(ct);

        await auditLogger.LogAsync(new AuditLogEntry
        {
            Action = "admin_create_module",
            Module = "subscriptions",
            Status = "success",
            ResourceId = moduleResult.Value!.Id.ToString(),
            ResourceType = "access_module"
        }, ct);

        return Result<ModuleSummaryDto>.Success(moduleResult.Value.ToSummaryDto());
    }
}

internal sealed class AdminCreateTierHandler(
    AppDbContext db,
    IAuditLogger auditLogger) : IRequestHandler<AdminCreateTierCommand, Result<SubscriptionTierDto>>
{
    public async Task<Result<SubscriptionTierDto>> Handle(AdminCreateTierCommand request, CancellationToken ct)
    {
        if (!await db.AccessModules.AnyAsync(x => x.Id == request.ModuleId && x.IsActive, ct))
        {
            return Result<SubscriptionTierDto>.Failure("Module not found.");
        }

        var tierResult = SubscriptionTier.Create(request.ModuleId, request.Name, request.Description, request.DisplayOrder);
        if (tierResult.IsFailure)
        {
            return Result<SubscriptionTierDto>.Failure(tierResult.Error!);
        }

        db.SubscriptionTiers.Add(tierResult.Value!);
        await db.SaveChangesAsync(ct);

        await auditLogger.LogAsync(new AuditLogEntry
        {
            Action = "admin_create_tier",
            Module = "subscriptions",
            Status = "success",
            ResourceId = tierResult.Value!.Id.ToString(),
            ResourceType = "subscription_tier"
        }, ct);

        return Result<SubscriptionTierDto>.Success(tierResult.Value.ToDto());
    }
}

internal sealed class AdminUpdateTierPricesHandler(
    AppDbContext db,
    ICacheService cacheService,
    IAuditLogger auditLogger) : IRequestHandler<AdminUpdateTierPricesCommand, Result>
{
    public async Task<Result> Handle(AdminUpdateTierPricesCommand request, CancellationToken ct)
    {
        var tier = await db.SubscriptionTiers
            .Join(db.AccessModules, x => x.ModuleId, x => x.Id, (subscriptionTier, module) => new { Tier = subscriptionTier, Module = module })
            .FirstOrDefaultAsync(x => x.Tier.Id == request.TierId, ct);

        if (tier is null)
        {
            return Result.Failure("Tier not found.");
        }

        db.TierPrices.RemoveRange(await db.TierPrices.Where(x => x.TierId == request.TierId).ToListAsync(ct));

        foreach (var priceInput in request.Prices)
        {
            var priceResult = TierPrice.Create(request.TierId, priceInput.Currency, priceInput.Amount, priceInput.BillingCycle, priceInput.TrialDays);
            if (priceResult.IsFailure)
            {
                return Result.Failure(priceResult.Error!);
            }

            db.TierPrices.Add(priceResult.Value!);
        }

        await db.SaveChangesAsync(ct);
        await cacheService.RemoveAsync(CacheKeys.SubscriptionPlans(tier.Module.Slug), ct);

        await auditLogger.LogAsync(new AuditLogEntry
        {
            Action = "admin_update_tier_prices",
            Module = "subscriptions",
            Status = "success",
            ResourceId = request.TierId.ToString(),
            ResourceType = "subscription_tier"
        }, ct);

        return Result.Success();
    }
}

internal sealed class AdminGrantAccessHandler(
    AppDbContext db,
    IAuditLogger auditLogger,
    ICacheService cacheService) : IRequestHandler<AdminGrantAccessCommand, Result<UserSubscriptionDto>>
{
    public async Task<Result<UserSubscriptionDto>> Handle(AdminGrantAccessCommand request, CancellationToken ct)
    {
        var tier = await db.SubscriptionTiers
            .Join(db.AccessModules, x => x.ModuleId, x => x.Id, (subscriptionTier, module) => new { Tier = subscriptionTier, Module = module })
            .FirstOrDefaultAsync(x => x.Tier.Id == request.TierId, ct);

        if (tier is null)
        {
            return Result<UserSubscriptionDto>.Failure("Tier not found.");
        }

        if (!await db.Customers.AnyAsync(x => x.Id == request.CustomerId, ct))
        {
            return Result<UserSubscriptionDto>.Failure("Customer not found.");
        }

        var startsAt = DateTime.UtcNow;
        var subscriptionResult = UserSubscription.Create(
            request.CustomerId,
            tier.Module.Id,
            tier.Tier.Id,
            AccessSource.AdminGrant,
            startsAt,
            request.EndsAtUtc,
            request.Note);

        if (subscriptionResult.IsFailure)
        {
            return Result<UserSubscriptionDto>.Failure(subscriptionResult.Error!);
        }

        db.UserSubscriptions.Add(subscriptionResult.Value!);
        await db.SaveChangesAsync(ct);
        await cacheService.RemoveAsync(CacheKeys.UserAccess(request.CustomerId, tier.Module.Id), ct);

        await auditLogger.LogAsync(new AuditLogEntry
        {
            Action = "admin_grant_access",
            Module = "subscriptions",
            Status = "success",
            ResourceId = subscriptionResult.Value!.Id.ToString(),
            ResourceType = "user_subscription"
        }, ct);

        return Result<UserSubscriptionDto>.Success(new UserSubscriptionDto
        {
            Id = subscriptionResult.Value.Id,
            ModuleName = tier.Module.Name,
            TierName = tier.Tier.Name,
            Status = subscriptionResult.Value.Status,
            StartsAt = subscriptionResult.Value.StartsAt,
            EndsAt = subscriptionResult.Value.EndsAt
        });
    }
}

internal sealed class AdminExtendSubscriptionHandler(
    AppDbContext db,
    IAuditLogger auditLogger,
    ICacheService cacheService) : IRequestHandler<AdminExtendSubscriptionCommand, Result>
{
    public async Task<Result> Handle(AdminExtendSubscriptionCommand request, CancellationToken ct)
    {
        var subscription = await db.UserSubscriptions.FirstOrDefaultAsync(x => x.Id == request.SubscriptionId, ct);
        if (subscription is null)
        {
            return Result.Failure("Subscription not found.");
        }

        var result = subscription.Extend(request.EndsAtUtc, request.Note);
        if (result.IsFailure)
        {
            return result;
        }

        await db.SaveChangesAsync(ct);
        await cacheService.RemoveAsync(CacheKeys.UserAccess(subscription.CustomerId, subscription.ModuleId), ct);

        await auditLogger.LogAsync(new AuditLogEntry
        {
            Action = "admin_extend_access",
            Module = "subscriptions",
            Status = "success",
            ResourceId = subscription.Id.ToString(),
            ResourceType = "user_subscription"
        }, ct);

        return Result.Success();
    }
}

internal sealed class AdminRevokeSubscriptionHandler(
    AppDbContext db,
    IAuditLogger auditLogger,
    ICacheService cacheService) : IRequestHandler<AdminRevokeSubscriptionCommand, Result>
{
    public async Task<Result> Handle(AdminRevokeSubscriptionCommand request, CancellationToken ct)
    {
        var subscription = await db.UserSubscriptions.FirstOrDefaultAsync(x => x.Id == request.SubscriptionId, ct);
        if (subscription is null)
        {
            return Result.Failure("Subscription not found.");
        }

        var result = subscription.Revoke(request.Note);
        if (result.IsFailure)
        {
            return result;
        }

        await db.SaveChangesAsync(ct);
        await cacheService.RemoveAsync(CacheKeys.UserAccess(subscription.CustomerId, subscription.ModuleId), ct);

        await auditLogger.LogAsync(new AuditLogEntry
        {
            Action = "admin_revoke_access",
            Module = "subscriptions",
            Status = "success",
            ResourceId = subscription.Id.ToString(),
            ResourceType = "user_subscription"
        }, ct);

        return Result.Success();
    }
}

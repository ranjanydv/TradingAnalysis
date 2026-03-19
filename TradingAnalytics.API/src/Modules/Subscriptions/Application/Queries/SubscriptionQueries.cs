using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TradingAnalytics.Modules.Subscriptions.Application.Dtos;
using TradingAnalytics.Modules.Subscriptions.Application.Mappings;
using TradingAnalytics.Shared.Infrastructure.Cache;
using TradingAnalytics.Shared.Infrastructure.Persistence;
using TradingAnalytics.Shared.Kernel.Interfaces;
using TradingAnalytics.Shared.Kernel.Results;

namespace TradingAnalytics.Modules.Subscriptions.Application.Queries;

/// <summary>
/// Gets active subscription modules.
/// </summary>
public sealed record GetModulesQuery() : IRequest<Result<List<ModuleSummaryDto>>>;

/// <summary>
/// Gets module tiers by slug.
/// </summary>
public sealed record GetModuleCatalogQuery(string ModuleSlug) : IRequest<Result<ModuleDetailDto>>;

/// <summary>
/// Gets the current user's access for a module.
/// </summary>
public sealed record GetMyAccessQuery(Guid ModuleId) : IRequest<Result<UserAccessDto>>;

/// <summary>
/// Gets the current user's subscriptions.
/// </summary>
public sealed record GetMySubscriptionsQuery() : IRequest<Result<List<UserSubscriptionDto>>>;

internal sealed class GetModulesHandler(AppDbContext db) : IRequestHandler<GetModulesQuery, Result<List<ModuleSummaryDto>>>
{
    public async Task<Result<List<ModuleSummaryDto>>> Handle(GetModulesQuery request, CancellationToken ct)
    {
        var modules = await db.AccessModules
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        return Result<List<ModuleSummaryDto>>.Success(modules.Select(x => x.ToSummaryDto()).ToList());
    }
}

internal sealed class GetModuleCatalogHandler(
    AppDbContext db,
    ICacheService cacheService) : IRequestHandler<GetModuleCatalogQuery, Result<ModuleDetailDto>>
{
    public async Task<Result<ModuleDetailDto>> Handle(GetModuleCatalogQuery request, CancellationToken ct)
    {
        var slug = request.ModuleSlug.Trim().ToLowerInvariant();
        var module = await db.AccessModules.FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive, ct);
        if (module is null)
        {
            return Result<ModuleDetailDto>.Failure("Module not found.");
        }

        var cacheKey = CacheKeys.SubscriptionPlans(module.Slug);
        var dto = await cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var tiers = await db.SubscriptionTiers
                .Where(x => x.ModuleId == module.Id && x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync(ct);

            var tierIds = tiers.Select(x => x.Id).ToList();
            var prices = await db.TierPrices
                .Where(x => tierIds.Contains(x.TierId) && x.IsActive)
                .OrderBy(x => x.Amount)
                .ToListAsync(ct);

            return new ModuleDetailDto
            {
                Id = module.Id,
                Slug = module.Slug,
                Name = module.Name,
                Description = module.Description,
                Tiers = tiers.Select(tier => new SubscriptionTierDto
                {
                    Id = tier.Id,
                    Name = tier.Name,
                    Description = tier.Description,
                    DisplayOrder = tier.DisplayOrder,
                    Prices = prices.Where(x => x.TierId == tier.Id).Select(x => x.ToDto()).ToList()
                }).ToList()
            };
        }, TimeSpan.FromMinutes(15), ct);

        return Result<ModuleDetailDto>.Success(dto);
    }
}

internal sealed class GetMyAccessHandler(
    AppDbContext db,
    ICacheService cacheService,
    ICurrentUserService currentUser,
    IConfiguration config) : IRequestHandler<GetMyAccessQuery, Result<UserAccessDto>>
{
    public async Task<Result<UserAccessDto>> Handle(GetMyAccessQuery request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
        {
            return Result<UserAccessDto>.Failure("User is not authenticated.");
        }

        var ttlSeconds = config.GetValue("Cache:UserAccessTtlSeconds", 300);
        var cacheKey = CacheKeys.UserAccess(currentUser.UserId.Value, request.ModuleId);
        var dto = await cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var now = DateTime.UtcNow;
            var subscription = await db.UserSubscriptions
                .Where(x => x.CustomerId == currentUser.UserId.Value && x.ModuleId == request.ModuleId)
                .OrderByDescending(x => x.EndsAt)
                .FirstOrDefaultAsync(ct);

            if (subscription is null || !subscription.IsActiveAt(now))
            {
                return new UserAccessDto
                {
                    CustomerId = currentUser.UserId.Value,
                    ModuleId = request.ModuleId,
                    HasAccess = false
                };
            }

            return new UserAccessDto
            {
                CustomerId = subscription.CustomerId,
                ModuleId = subscription.ModuleId,
                SubscriptionId = subscription.Id,
                HasAccess = true,
                TierId = subscription.TierId,
                EndsAt = subscription.EndsAt,
                Source = subscription.Source
            };
        }, TimeSpan.FromSeconds(ttlSeconds), ct);

        return Result<UserAccessDto>.Success(dto);
    }
}

internal sealed class GetMySubscriptionsHandler(
    AppDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<GetMySubscriptionsQuery, Result<List<UserSubscriptionDto>>>
{
    public async Task<Result<List<UserSubscriptionDto>>> Handle(GetMySubscriptionsQuery request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
        {
            return Result<List<UserSubscriptionDto>>.Failure("User is not authenticated.");
        }

        var subscriptions = await db.UserSubscriptions
            .Where(x => x.CustomerId == currentUser.UserId.Value)
            .Join(db.AccessModules, x => x.ModuleId, x => x.Id, (subscription, module) => new { Subscription = subscription, Module = module })
            .Join(db.SubscriptionTiers, x => x.Subscription.TierId, x => x.Id, (joined, tier) => new { joined.Subscription, joined.Module, Tier = tier })
            .OrderByDescending(x => x.Subscription.EndsAt)
            .ToListAsync(ct);

        return Result<List<UserSubscriptionDto>>.Success(subscriptions.Select(x => new UserSubscriptionDto
        {
            Id = x.Subscription.Id,
            ModuleName = x.Module.Name,
            TierName = x.Tier.Name,
            Status = x.Subscription.Status,
            StartsAt = x.Subscription.StartsAt,
            EndsAt = x.Subscription.EndsAt
        }).ToList());
    }
}

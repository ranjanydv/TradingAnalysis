using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TradingAnalytics.Modules.Subscriptions;

/// <summary>
/// Registers subscription module services.
/// </summary>
public static class SubscriptionsModule
{
    /// <summary>
    /// Adds the subscriptions module.
    /// </summary>
    public static IServiceCollection AddSubscriptionsModule(this IServiceCollection services, IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining(typeof(SubscriptionsModule)));
        services.AddValidatorsFromAssemblyContaining(typeof(SubscriptionsModule));
        return services;
    }
}

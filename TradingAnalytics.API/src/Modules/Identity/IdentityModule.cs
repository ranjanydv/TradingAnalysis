using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TradingAnalytics.Modules.Identity;

/// <summary>
/// Registers Identity module services.
/// </summary>
public static class IdentityModule
{
    /// <summary>
    /// Adds the Identity module.
    /// </summary>
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining(typeof(IdentityModule)));
        services.AddValidatorsFromAssemblyContaining(typeof(IdentityModule));
        return services;
    }
}

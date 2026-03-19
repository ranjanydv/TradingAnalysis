using TradingAnalytics.Shared.Kernel.Interfaces;

namespace TradingAnalytics.Modules.Identity.Domain.Events;

/// <summary>
/// Raised when a customer is banned.
/// </summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="Reason">The ban reason.</param>
public sealed record CustomerBannedEvent(Guid CustomerId, string Reason) : IDomainEvent;

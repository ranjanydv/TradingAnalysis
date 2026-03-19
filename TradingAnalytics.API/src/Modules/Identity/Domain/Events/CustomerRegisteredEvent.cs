using TradingAnalytics.Shared.Kernel.Interfaces;

namespace TradingAnalytics.Modules.Identity.Domain.Events;

/// <summary>
/// Raised when a customer is registered.
/// </summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="Email">The optional email.</param>
/// <param name="Phone">The optional phone.</param>
public sealed record CustomerRegisteredEvent(Guid CustomerId, string? Email, string? Phone) : IDomainEvent;

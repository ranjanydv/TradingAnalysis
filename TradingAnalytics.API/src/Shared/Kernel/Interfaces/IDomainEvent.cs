using MediatR;

namespace TradingAnalytics.Shared.Kernel.Interfaces;

/// <summary>
/// Marks a notification as a domain event.
/// </summary>
public interface IDomainEvent : INotification
{
}

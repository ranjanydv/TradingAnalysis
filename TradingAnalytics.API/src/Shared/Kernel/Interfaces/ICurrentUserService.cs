namespace TradingAnalytics.Shared.Kernel.Interfaces;

/// <summary>
/// Provides information about the currently authenticated actor.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user identifier.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the actor type, such as customer or admin.
    /// </summary>
    string? ActorType { get; }

    /// <summary>
    /// Gets the current role.
    /// </summary>
    string? Role { get; }

    /// <summary>
    /// Gets a value indicating whether the actor is an administrator.
    /// </summary>
    bool IsAdmin { get; }

    /// <summary>
    /// Gets a value indicating whether the actor is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}

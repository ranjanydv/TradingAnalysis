namespace TradingAnalytics.Shared.Infrastructure.Session;

/// <summary>
/// Manages session state in a fast-access store.
/// </summary>
public interface ISessionStore
{
    /// <summary>
    /// Stores session data.
    /// </summary>
    Task SetAsync(string token, SessionData data, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>
    /// Gets session data by token.
    /// </summary>
    Task<SessionData?> GetAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Removes a single session.
    /// </summary>
    Task RemoveAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Removes all sessions for a user.
    /// </summary>
    Task RemoveAllForUserAsync(Guid userId, CancellationToken ct = default);
}
